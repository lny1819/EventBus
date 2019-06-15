using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;

namespace YiDian.EventBus.MQ
{
    public class DirectEventBus : IDirectEventBus, IDisposable
    {
        protected string BROKER_NAME = "amq.direct";
        protected string AUTOFAC_SCOPE_NAME = "DirectEventBus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        IModel _pubChannel;
        readonly IQpsCounter _counter;
        readonly int _retryCount;
        readonly ILogger<DirectEventBus> _logger;
        readonly List<ConsumerConfig> consumerInfos;
        readonly ConcurrentQueue<BasicDeliverEventArgs> __processQueue;

        public DirectEventBus(IRabbitMQPersistentConnection persistentConnection, ILogger<DirectEventBus> logger, ILifetimeScope autofac, IEventBusSubscriptionsManager subsManager, int retryCount = 5, int cacheCount = 100)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _persistentConnection.OnConnectRecovery += _persistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));

            __processQueue = new ConcurrentQueue<BasicDeliverEventArgs>();
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
            consumerInfos = new List<ConsumerConfig>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac, AUTOFAC_SCOPE_NAME);
            _retryCount = retryCount;
            CreatePublishChannel();
        }

        private void _persistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {
            foreach (var consumerinfo in consumerInfos)
            {
                CreateConsumerChannel(false, consumerinfo);
            }
        }

        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
        }
        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var queueName = GetEventConsumerQueue(eventName);
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);
            }
        }

        private string GetEventConsumerQueue(string eventName)
        {
            throw new NotImplementedException();
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
            var eventName = _subsManager.GetEventKey<T>();
            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            _pubChannel.BasicPublish(exchange: BROKER_NAME, routingKey: eventName, basicProperties: null, body: body);
        }
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            eventName = _subsManager.GetEventKey(eventName);
            if (string.IsNullOrEmpty(eventName))
            {
                _logger.LogError($"can not find consumer handlers by {eventName}");
                return;
            }
            _subsManager.AddDynamicSubscription<TH>(eventName);
            DoInternalSubscription(eventName);
        }
        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T, TH>();
        }
        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using (var channel = _persistentConnection.CreateModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent
        {
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }
            if (_pubChannel != null)
            {
                _pubChannel.Dispose();
            }
            _subsManager.Clear();
        }

        private void CreateConsumerChannel(ConsumerConfig config)
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
        private void ProcessEvent(BasicDeliverEventArgs ea)
        {
            var eventName = ea.RoutingKey;
            if (!_subsManager.HasSubscriptionsForEvent(eventName)) return;
            var message = Encoding.UTF8.GetString(ea.Body);
            object integrationEvent = null;
            var eventType = _subsManager.GetEventTypeByName(eventName);
            if (eventType != null && eventType.IsSubclassOf(typeof(IntegrationMQEvent)))
                integrationEvent = JsonConvert.DeserializeObject(message, eventType);
            var subscriptions = _subsManager.GetHandlersForEvent(eventName).ToList();
            var tasks = new List<Task<bool>>(subscriptions.Count);
            foreach (var subscription in subscriptions)
            {
                if (subscription.IsDynamic)
                {
                    hanlerCacheMgr.GetDynamicHandler(subscription.HandlerType, out IDynamicBytesHandler handler, out ILifetimeScope scope);
                    tasks.Add(handler.Handle(message).ContinueWith(e =>
                    {
                        var flag = ProcessExceptionHandler(e);
                        hanlerCacheMgr.ResteDymaicHandler(handler, subscription.HandlerType, scope);
                        return flag;
                    }));
                }
                else
                {
                    hanlerCacheMgr.GetIIntegrationEventHandler(subscription.HandlerType, out IIntegrationEventHandler handler, out ILifetimeScope scope);
                    tasks.Add(((Task)subscription.Handler(handler, new object[] { integrationEvent }))
                    .ContinueWith(e =>
                    {
                        var flag = ProcessExceptionHandler(e);
                        hanlerCacheMgr.ResteTypeHandler(handler, subscription.HandlerType, scope);
                        return flag;
                    }));
                }
            }
            if (_autoAck) return;
            Task.Factory.ContinueWhenAll(tasks.ToArray(), e =>
            {
                bool flag = false;
                e.ToList().ForEach(x => flag = flag | x.Result);
                if (flag) _consumerChannel.BasicAck(ea.DeliveryTag, false);
                else _consumerChannel.BasicNack(ea.DeliveryTag, false, true);
            });
        }
        bool ProcessExceptionHandler(Task task)
        {
            if (task.Status == TaskStatus.Faulted)
            {
                if (ProcessException == null) _logger?.LogError(task.Exception.ToString());
                else ProcessException(task.Exception);
                return false;
            }
            return true;
        }
        public void StartConsumer(string queueName, Action<DirectSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            config = new ConsumerConfig<DirectSubscriber>(this, __processQueue)
            {
                AutoAck = autoAck,
                MaxLength = queueLength,
                Durable = durable,
                AutoDel = autodel,
                FetchCount = fetchCount,
                Name = queueName,
                SubAction = action
            };
            consumerInfos.Add(config);
            CreateConsumerChannel(config);
        }

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
    }
}
