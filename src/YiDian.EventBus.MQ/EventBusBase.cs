using Autofac;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YiDian.EventBus.MQ
{
    public abstract class EventBusBase<TEventBus, TSub> : IEventBus, IDisposable where TEventBus : IEventBus where TSub : Subscriber<TEventBus>
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        readonly int _retryCount;
        readonly ILogger<TEventBus> _logger;
        readonly ThreadChannels channels = ThreadChannels.Default;
        readonly IEventBusSubscriptionsManagerFactory _subsFactory;
        IModel _pubChannel;

        public event EventHandler<Exception> OnUncatchException;

        protected readonly List<ConsumerConfig<TEventBus, TSub>> consumerInfos;
        protected readonly ISeralize __seralize;

        internal EventBusBase(ILogger<TEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection = null, IEventBusSubscriptionsManagerFactory factory = null, ISeralize seralize = null, int retryCount = 5, int cacheCount = 100)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _persistentConnection.OnConnectRecovery += _persistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger<TEventBus>));
            __seralize = seralize ?? throw new ArgumentNullException(nameof(ISeralize));
            _subsFactory = factory ?? new InMemorySubFactory();
            consumerInfos = new List<ConsumerConfig<TEventBus, TSub>>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac, AUTOFAC_SCOPE_NAME);
            channels.UnCatchedException += LogError;
            _retryCount = retryCount;
            CreatePublishChannel();
        }

        protected IEventBusSubscriptionsManager GetSubscriber(string queueName)
        {
            var submgr = _subsFactory.GetOrCreateByQueue(queueName);
            submgr.OnEventRemoved += SubsManager_OnEventRemoved;
            return submgr;
        }
        public abstract string BROKER_NAME { get; }
        public abstract string AUTOFAC_SCOPE_NAME { get; }
        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
        }


        #region Mq Sub And UnSub
        void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            var mgr = (IEventBusSubscriptionsManager)sender;
            var queueName = mgr.QueueName;
            DoInternalUnSub(queueName, eventName);
        }
        protected void DoInternalUnSub(string queueName, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);
            }
        }

        protected void DoInternalSubscription(IEventBusSubscriptionsManager mgr, string eventName)
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
        protected void PublishBase(string eventName, byte[] body)
        {
            if (_pubChannel == null) return;
            _pubChannel.BasicPublish(exchange: BROKER_NAME,
                             routingKey: eventName,
                             basicProperties: null,
                             body: body);
        }
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
            var message = __seralize.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            PublishBase(eventName, body);
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

        private void LogError(Exception ex)
        {
            if (OnUncatchException != null)
                OnUncatchException(this, ex);
            else _logger.LogError(ex.ToString());
        }
        private void _persistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {
            //foreach (var consumerinfo in consumerInfos)
            //{
            //    CreateConsumerChannel(consumerinfo);
            //}
        }
        protected virtual void CreateConsumerChannel(ConsumerConfig<TEventBus, TSub> config)
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
            config.Start(channel, (c, e) =>
            {
                var item = new QueueItem<TEventBus, TSub>(c, e);
                channels.QueueWorkItemInternal(StartProcess, item);
            });
        }

        private void StartProcess(object obj)
        {
            var item = (QueueItem<TEventBus, TSub>)obj;
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

        private void ProcessEvent(IEnumerable<SubscriptionInfo> subInfos, QueueItem<TEventBus, TSub> msg)
        {
            var ea = msg.Event;
            int xxx = 0;
            var config = msg.ConsumerConfig;
            foreach (var subinfo in subInfos)
            {
                if (subinfo.IsDynamic)
                {
                    hanlerCacheMgr.GetDynamicHandler(subinfo.HandlerType, out IDynamicBytesHandler handler, out ILifetimeScope scope);
                    var task = handler.Handle(ea.RoutingKey, ea.Body);
                    var res = task.ConfigureAwait(false);
                    var waiter = res.GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        hanlerCacheMgr.ResteDymaicHandler(handler, subinfo.HandlerType, scope);
                        if (task.IsFaulted) LogError(task.Exception);
                        if (!config.AutoAck && task.IsCompletedSuccessfully && Interlocked.CompareExchange(ref xxx, 1, 0) == 0)
                        {
                            if (waiter.IsCompleted && waiter.GetResult()) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                            else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                        }
                    });
                }
                else
                {
                    var integrationEvent = DeserializeObject(ea.Body, subinfo.EventType);
                    hanlerCacheMgr.GetIIntegrationEventHandler(subinfo.HandlerType, out IIntegrationEventHandler handler, out ILifetimeScope scope);
                    var task = (Task<bool>)subinfo.Handler(handler, new object[] { integrationEvent });
                    var res = task.ConfigureAwait(false);
                    var waiter = res.GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        hanlerCacheMgr.ResteTypeHandler(handler, subinfo.HandlerType, scope);
                        if (task.IsFaulted) LogError(task.Exception);
                        if (!config.AutoAck && task.IsCompletedSuccessfully && Interlocked.CompareExchange(ref xxx, 1, 0) == 0)
                        {
                            if (waiter.IsCompleted && waiter.GetResult()) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                            else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                        }
                    });
                }
            }
        }
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private object DeserializeObject(byte[] body, Type type)
        {
            return __seralize.DeserializeObject(Encoding.UTF8.GetString(body), type);
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
