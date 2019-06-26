using Autofac;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace YiDian.EventBus.MQ
{
    public abstract class EventBusBase<TEventBus, TSub> : IEventBus, IDisposable where TEventBus : IEventBus where TSub : Subscriber<TEventBus>
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        readonly int _retryCount;
        readonly ILogger<TEventBus> _logger;
        readonly ThreadChannels channels = ThreadChannels.Default;
        readonly IEventBusSubManagerFactory _subsFactory;
        readonly PublishPool publishPool = null;

        public event EventHandler<Exception> OnUncatchException;

        protected readonly List<ConsumerConfig<TEventBus, TSub>> consumerInfos;
        protected readonly IEventSeralize __seralize;

        internal EventBusBase(ILogger<TEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection, ILogger<IEventBusSubManager> sub_logger, IEventBusSubManagerFactory factory = null, IEventSeralize seralize = null, int retryCount = 5, int cacheCount = 100)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _persistentConnection.OnConnectRecovery += _persistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger<TEventBus>));
            __seralize = seralize ?? throw new ArgumentNullException(nameof(IEventSeralize));
            _subsFactory = factory ?? new InMemorySubFactory(persistentConnection.EventsManager, sub_logger);
            consumerInfos = new List<ConsumerConfig<TEventBus, TSub>>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac, AUTOFAC_SCOPE_NAME);
            channels.UnCatchedException += LogError;
            _retryCount = retryCount;
            publishPool = new PublishPool(_persistentConnection, __seralize, BROKER_NAME);
        }

        protected IEventBusSubManager GetSubscriber(string queueName)
        {
            var submgr = _subsFactory.GetOrCreateByQueue(queueName);
            submgr.OnEventRemoved += SubsManager_OnEventRemoved;
            submgr.OnEventAdd += Submgr_OnEventAdd;
            return submgr;
        }

        public abstract string BROKER_NAME { get; }
        public abstract string AUTOFAC_SCOPE_NAME { get; }
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
        void Submgr_OnEventAdd(object sender, string eventName)
        {
            var mgr = (IEventBusSubManager)sender;
            var queueName = mgr.QueueName;
            DoInternalSubscription(queueName, eventName);
        }
        void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            var mgr = (IEventBusSubManager)sender;
            var queueName = mgr.QueueName;
            DoInternalUnSub(queueName, eventName);
        }
        void DoInternalUnSub(string queueName, string eventName)
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
        void DoInternalSubscription(string queueName, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueBind(queue: queueName,
                                  exchange: BROKER_NAME,
                                  routingKey: eventName);
            }
        }
        #endregion

        #region Publish
        IEventBusSubManager PubSubMgr
        {
            get
            {
                return _subsFactory.GetOrCreateByQueue("publish");
            }
        }
        public void Publish<T>(T @event, Func<string, string> key_handler, bool enableTransaction = false) where T : IMQEvent
        {
            var pubkey1 = PubSubMgr.GetEventKey<T>();
            var pubkey2 = key_handler(pubkey1);
            if (string.IsNullOrEmpty(pubkey1) || string.IsNullOrEmpty(pubkey2))
            {
                _logger.LogError($"can not find the publish key of type:{typeof(T).Name}");
                return;
            }
            publishPool.Send(@event, pubkey2, enableTransaction);
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
            var eventName = GetEventKeyFromRoutingKey(item.Event.RoutingKey);
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
                    hanlerCacheMgr.GetDynamicHandler(subinfo.HandlerType, out IBytesHandler handler, out ILifetimeScope scope);
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
                    object integrationEvent = null;
                    using (var ms = new MemoryStream(ea.Body))
                    {
                        integrationEvent = __seralize.DeserializeObject(ms, subinfo.EventType);
                    }
                    hanlerCacheMgr.GetIIntegrationEventHandler(subinfo.HandlerType, out IEventHandler handler, out ILifetimeScope scope);
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
            publishPool.Dispose();
            _persistentConnection.Dispose();
        }

    }
}
