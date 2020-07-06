using Autofac;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using YiDian.Soa.Sp;

namespace YiDian.EventBus.MQ
{
    public abstract class EventBusBase<TEventBus, TSub> : IEventBus, IDisposable where TEventBus : IEventBus where TSub : Subscriber<TEventBus>
    {
        private readonly IRabbitMQPersistentConnection _conn;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        readonly int _retryCount;
        readonly ILogger<TEventBus> _logger;
        readonly ThreadDispatcher<QueueItem<TEventBus, TSub>> channels;
        PublishPool publishPool = null;
        readonly IEventBusSubManager _pub_sub;

        public event EventHandler<Exception> OnUncatchException;

        protected readonly List<ConsumerConfig<TEventBus, TSub>> consumerInfos;
        protected readonly IEventSeralize __seralize;

        internal EventBusBase(ILogger<TEventBus> logger, IServiceProvider autofac, IEventSeralize seralize, IRabbitMQPersistentConnection persistentConnection, int retryCount = 5, int cacheCount = 100)
        {
            _conn = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            ConnectionName = _conn.Name;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger<TEventBus>));
            __seralize = seralize ?? throw new ArgumentNullException(nameof(IEventSeralize));
            consumerInfos = new List<ConsumerConfig<TEventBus, TSub>>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac);
            _pub_sub = persistentConnection.SubsFactory.GetOrCreateByQueue("publish");
            channels = new ThreadDispatcher<QueueItem<TEventBus, TSub>>(StartProcess, Math.Min(8, Environment.ProcessorCount / 2))
            {
                UnCatchedException = LogError
            };
            _retryCount = retryCount;
        }

        protected IEventBusSubManager GetSubscriber(string queueName)
        {
            var submgr = _conn.SubsFactory.GetOrCreateByQueue(queueName);
            submgr.OnEventRemoved += SubsManager_OnEventRemoved;
            submgr.OnEventAdd += Submgr_OnEventAdd;
            return submgr;
        }

        public string ConnectionName { get; }
        public abstract string BROKER_NAME { get; }
        public abstract void Publish<T>(T @event, bool enableTransaction = false) where T : IMQEvent;
        public abstract string GetEventKeyFromRoutingKey(string routingKey);
        public abstract void Subscribe<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        public abstract void Unsubscribe<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
        }
        #region Mq Sub And UnSub
        void Submgr_OnEventAdd(object sender, (string, string) events)
        {
            var brokerName = events.Item2;
            if (BROKER_NAME != brokerName) return;
            var eventName = events.Item1;
            var mgr = (IEventBusSubManager)sender;
            var queueName = mgr.QueueName;
            DoInternalSubscription(queueName, eventName, brokerName);
        }
        void SubsManager_OnEventRemoved(object sender, (string, string) events)
        {
            var brokerName = events.Item2;
            if (BROKER_NAME != brokerName) return;
            var eventName = events.Item1;
            var mgr = (IEventBusSubManager)sender;
            var queueName = mgr.QueueName;
            DoInternalUnSub(queueName, eventName, brokerName);
        }
        void DoInternalUnSub(string queueName, string eventName, string borokerName)
        {
            if (!_conn.IsConnected)
            {
                _conn.TryConnect();
            }
            using (var channel = _conn.CreateModel())
            {
                channel.QueueUnbind(queue: queueName,
                    exchange: borokerName,
                    routingKey: eventName);
            }
        }
        void DoInternalSubscription(string queueName, string eventName, string brokerName)
        {
            if (!_conn.IsConnected)
            {
                _conn.TryConnect();
            }

            using (var channel = _conn.CreateModel())
            {
                channel.QueueBind(queue: queueName,
                                  exchange: brokerName,
                                  routingKey: eventName);
            }
        }
        #endregion

        #region Publish

        public void PublishWithKey<T>(T @event, string key, bool enableTransaction = false) where T : IMQEvent
        {
            Publish(@event, (x) => key, enableTransaction);
        }
        protected void Publish<T>(T @event, Func<string, string> key_handler, bool enableTransaction = false) where T : IMQEvent
        {
            var pubkey1 = _pub_sub.GetEventKey(@event.GetType());
            var pubkey2 = key_handler(pubkey1);
            if (string.IsNullOrEmpty(pubkey1) || string.IsNullOrEmpty(pubkey2))
            {
                _logger.LogError($"can not find the publish key of type:{typeof(T).Name}");
                return;
            }
            try
            {
                if (publishPool == null)
                {
                    lock (typeof(PublishPool))
                    {
                        if (publishPool == null)
                            publishPool = new PublishPool(_conn, __seralize, BROKER_NAME);
                    }
                }
                publishPool.Send(@event, pubkey2, enableTransaction);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
        #endregion

        #region Consumer

        public void Start(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                _logger.LogInformation("the queueName can not be empth");
                return;
            }
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config == null)
            {
                _logger.LogInformation("can not find the consumerinfo be named " + queueName);
                return;
            }
            config.Start();
        }
        protected void LogError(Exception ex)
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
        protected virtual void CreateConsumerChannel(ConsumerConfig<TEventBus, TSub> config, bool autoStart)
        {
            if (!_conn.IsConnected)
            {
                _conn.TryConnect();
            }
            var dic = new Dictionary<string, object>
            {
                ["x-max-length"] = config.MaxLength
            };
            var channel = _conn.CreateModel();
            channel.QueueDeclare(queue: config.Name,
                                 durable: config.Durable,
                                 exclusive: false,
                                 autoDelete: config.AutoDel,
                                 arguments: dic);
            channel.BasicQos(0, config.FetchCount, false);
            channel.CallbackException += (sender, ea) =>
            {
                CreateConsumerChannel(config, true);
            };
            config.Register(channel, (c, e) =>
            {
                var item = new QueueItem<TEventBus, TSub>(c, e);
                channels.QueueWorkItemInternal(item);
            }, autoStart);
        }
        private void StartProcess(QueueItem<TEventBus, TSub> item)
        {
            var eventName = GetEventKeyFromRoutingKey(item.Event.RoutingKey);
            var consumer = item.ConsumerConfig;
            var mgr = consumer.GetSubMgr();
            var handlers = mgr.GetHandlersForEvent(eventName);
            var ider = handlers.GetEnumerator();
            var flag = ider.MoveNext();
            if (!flag)
            {
                _logger.LogWarning("routingkey=" + item.Event.RoutingKey + ",eventName=" + eventName + ",sub event but not set handlers");
                return;
            }
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
            var ider = subInfos.GetEnumerator();
            while (ider.MoveNext())
            {
                var subinfo = ider.Current;
                if (subinfo.IsDynamic)
                {
                    hanlerCacheMgr.GetDynamicHandler(subinfo.HandlerType, out IBytesHandler handler, out IServiceScope scope);
                    var task = handler.Handle(ea.RoutingKey, ea.Body);
                    var res = task.ConfigureAwait(false);
                    var waiter = res.GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        hanlerCacheMgr.ResteDymaicHandler(handler, subinfo.HandlerType, scope);
                        if (task.IsFaulted) LogError(task.Exception);
                        if (!config.AutoAck && task.IsCompletedSuccessfully)
                        {
                            if (waiter.IsCompleted && waiter.GetResult())
                            {
                                if (Interlocked.CompareExchange(ref xxx, 1, 0) == 0) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                            }
                            else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                        }
                    });
                }
                else
                {
                    var integrationEvent = __seralize.DeserializeObject(ea.Body, subinfo.EventType);
                    hanlerCacheMgr.GetIIntegrationEventHandler(subinfo.HandlerType, out IEventHandler handler, out IServiceScope scope);
                    var task = (Task<bool>)subinfo.Handler(handler, new object[] { integrationEvent });
                    var res = task.ConfigureAwait(false);
                    var waiter = res.GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        hanlerCacheMgr.ResteTypeHandler(handler, subinfo.HandlerType, scope);
                        if (task.IsFaulted) LogError(task.Exception);
                        if (!config.AutoAck && task.IsCompletedSuccessfully)
                        {
                            if (waiter.IsCompleted && waiter.GetResult())
                            {
                                if (Interlocked.CompareExchange(ref xxx, 1, 0) == 0) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                            }
                            else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                        }
                    });
                }
            }
        }
        #endregion

        public void DeleteQueue(string queuename, bool force)
        {
            if (!_conn.IsConnected)
            {
                _conn.TryConnect();
            }
            var channel = _conn.CreateModel();
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
            publishPool?.Dispose();
            _conn.Dispose();
        }

    }
}
