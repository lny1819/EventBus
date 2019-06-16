using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.CompilerServices;

namespace YiDian.EventBus.MQ
{
    public class DirectEventBus : IDirectEventBus, IDisposable
    {
        const string BROKER_NAME = "amq.direct";
        const string AUTOFAC_SCOPE_NAME = "DirectEventBus";
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManagerFactory _subsFactory;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        IModel _pubChannel;
        readonly int _retryCount;
        readonly ILogger<IEventBus> _logger;
        readonly List<ConsumerConfig<IDirectEventBus, DirectSubscriber>> consumerInfos;
        readonly ThreadChannels channels = ThreadChannels.Default;
        //readonly ConcurrentQueue<QueueItem<IDirectEventBus, DirectSubscriber>> __processQueue;

        public DirectEventBus(ILogger<IEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection = null, IEventBusSubscriptionsManagerFactory factory = null, int retryCount = 5, int cacheCount = 100)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _persistentConnection.OnConnectRecovery += _persistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            //__processQueue = new ConcurrentQueue<QueueItem<IDirectEventBus, DirectSubscriber>>();
            _subsFactory = factory ?? new InMemorySubFactory();
            consumerInfos = new List<ConsumerConfig<IDirectEventBus, DirectSubscriber>>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac, AUTOFAC_SCOPE_NAME);
            _retryCount = retryCount;
            CreatePublishChannel();
        }
        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
        }
        #region Mq Sub And UnSub
        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var mgr = (IEventBusSubscriptionsManager)sender;
            var queueName = mgr.QueueName;
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);
            }
        }

        private void DoInternalSubscription(IEventBusSubscriptionsManager mgr, string eventName)
        {
            var containsKey = mgr.SubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using (var channel = _persistentConnection.CreateModel())
                {
                    channel.QueueBind(queue: mgr.QueueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }
        #endregion

        #region Publish
        void CreatePublishChannel()
        {
            if (_pubChannel == null || _pubChannel.IsClosed)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                //_pubChannel.ConfirmSelect();
                _pubChannel = _persistentConnection.CreateModel();
                _pubChannel.CallbackException += (sender, ea) =>
                {
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }
        public void Publish<T>(T @event, bool enableTx) where T : IntegrationMQEvent
        {
            var pub_sub_mgr = _subsFactory.GetOrCreateByQueue("publish");
            var eventName = pub_sub_mgr.GetEventKey<T>();
            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            _pubChannel.BasicPublish(exchange: BROKER_NAME, routingKey: eventName, basicProperties: null, body: body);
        }
        #endregion

        #region Mgr Sub And UnSub
        public void Subscribe<TH>(string queueName, string eventName)
            where TH : IDynamicBytesHandler
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    eventName = mgr.GetEventKey(eventName);
                    if (string.IsNullOrEmpty(eventName))
                    {
                        _logger.LogError($"can not find consumer handlers by {eventName}");
                        return;
                    }
                    mgr.AddSubscription<TH>(eventName);
                    DoInternalSubscription(mgr, eventName);
                    break;
                }
            }
        }
        public void Subscribe<T, TH>(string queueName)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventName = mgr.GetEventKey<T>();
                    mgr.AddSubscription<T, TH>();
                    DoInternalSubscription(mgr, eventName);
                    break;
                }
            }
        }

        public void Unsubscribe<T, TH>(string queueName)
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    item.Unsubscribe<T, TH>();
                    break;
                }
            }
        }

        public void Unsubscribe<TH>(string queueName, string eventName)
            where TH : IDynamicBytesHandler
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    item.Unsubscribe<TH>(eventName);
                    break;
                }
            }
        }
        #endregion

        #region Consumer
        private void _persistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {
            //foreach (var consumerinfo in consumerInfos)
            //{
            //    CreateConsumerChannel(consumerinfo);
            //}
        }
        private void Config_OnReceive(object sender, BasicDeliverEventArgs e)
        {
            var config = (ConsumerConfig<IDirectEventBus, DirectSubscriber>)sender;
            var item = new QueueItem<IDirectEventBus, DirectSubscriber>(config, e);
            channels.QueueWorkItemInternal(StartProcess, item);
            //__processQueue.Enqueue(new QueueItem<IDirectEventBus, DirectSubscriber>(config, e));
            //StartProcess();
            //CheckQueue();
        }
        private void CreateConsumerChannel(ConsumerConfig<IDirectEventBus, DirectSubscriber> config)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var dic = new Dictionary<string, object>
            {
                ["x-max-length"] = config.MaxLength
            };
            var channel = _persistentConnection.CreateModel();
            channel.QueueDeclare(queue: config.Name,
                                 durable: config.Durable,
                                 exclusive: false,
                                 autoDelete: config.AutoDel,
                                 arguments: dic);
            channel.BasicQos(0, config.FetchCount, false);
            channel.CallbackException += (sender, ea) =>
            {
                CreateConsumerChannel(config);
            };
            config.Start(channel);
        }

        //private void CheckQueue()
        //{
        //    if (__processQueue.Count > 10000)
        //        Thread.Sleep(30);
        //    else if (__processQueue.Count > 20000)
        //    {
        //        Thread.Sleep(1000);
        //    }
        //    else if (__processQueue.Count > 30000)
        //    {
        //        Thread.Sleep(5000);
        //    }
        //}
        private void StartProcess(object obj)
        {
            var item = (QueueItem<IDirectEventBus, DirectSubscriber>)obj;
            var eventName = item.Event.RoutingKey;
            var consumer = item.ConsumerConfig;
            var mgr = consumer.GetSubMgr();
            var handlers = mgr.GetHandlersForEvent(eventName);
            if (handlers == null) return;
             ProcessEvent(handlers, item);
        }
        private void AckChannel(object obj)
        {
            var item = (QueueItem<IDirectEventBus, DirectSubscriber>)obj;
            var eventName = item.Event.RoutingKey;
            var consumer = item.ConsumerConfig;
            var mgr = consumer.GetSubMgr();
            var handlers = mgr.GetHandlersForEvent(eventName);
            if (handlers == null) return;
            var ea = item.Event;
            var config = item.ConsumerConfig;
            config.GetChannel().BasicAck(ea.DeliveryTag, false);
        }

        private async void ProcessEvent(IEnumerable<SubscriptionInfo> subInfos, QueueItem<IDirectEventBus, DirectSubscriber> msg)
        {
            var ea = msg.Event;
            var config = msg.ConsumerConfig;
            foreach (var subinfo in subInfos)
            {
                if (subinfo.IsDynamic)
                {
                    hanlerCacheMgr.GetDynamicHandler(subinfo.HandlerType, out IDynamicBytesHandler handler, out ILifetimeScope scope);
                    var res = handler.Handle(ea.RoutingKey, ea.Body).ConfigureAwait(false);
                    await res;
                    hanlerCacheMgr.ResteDymaicHandler(handler, subinfo.HandlerType, scope);
                    var x = res.GetAwaiter();
                    // if (x.Status == TaskStatus.Faulted) _logger.LogError(x.Exception.ToString());
                    if (!config.AutoAck)
                    {
                        if (x.IsCompleted && x.GetResult()) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                        else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                    }
                    //.ConfigureAwait(false);
                }
                else
                {
                    var integrationEvent = DeserializeObject(ea.Body, subinfo.EventType);
                    hanlerCacheMgr.GetIIntegrationEventHandler(subinfo.HandlerType, out IIntegrationEventHandler handler, out ILifetimeScope scope);
                    var res = ((Task<bool>)subinfo.Handler(handler, new object[] { integrationEvent })).ConfigureAwait(false);
                    await res;
                    hanlerCacheMgr.ResteTypeHandler(handler, subinfo.HandlerType, scope);
                    var x = res.GetAwaiter();
                    if (!config.AutoAck)
                    {
                        if (x.IsCompleted && x.GetResult()) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                        else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                    }
                }
            }
        }
        [MethodImpl(methodImplOptions:MethodImplOptions.AggressiveInlining)]
        private object DeserializeObject(byte[] body, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(body), type);
        }
        public void StartConsumer(string queueName, Action<DirectSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new DirectSubscriber(this, queueName);
            var submgr = _subsFactory.GetOrCreateByQueue(queueName);
            submgr.OnEventRemoved += SubsManager_OnEventRemoved;
            config = new ConsumerConfig<IDirectEventBus, DirectSubscriber>(scriber, submgr)
            {
                AutoAck = autoAck,
                MaxLength = queueLength,
                Durable = durable,
                AutoDel = autodel,
                FetchCount = fetchCount,
                Name = queueName,
                SubAction = action
            };
            config.OnReceive += Config_OnReceive;
            consumerInfos.Add(config);
            CreateConsumerChannel(config);
        }
        #endregion

        public void DeleteQueue(string queuename, bool force)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var channel = _persistentConnection.CreateModel();
            if (force)
                channel.QueueDelete(queuename, false, false);
            else
                channel.QueueDelete(queuename, true, true);
            channel.Close();
        }

        public void Dispose()
        {
            foreach (var item in consumerInfos)
            {
                item.Dispose();
            }
            consumerInfos.Clear();
            _pubChannel.Dispose();
            _persistentConnection.Dispose();
        }
    }
}
