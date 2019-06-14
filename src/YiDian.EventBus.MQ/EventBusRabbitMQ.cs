using Autofac;
using Microsoft.Extensions.Logging;
using YiDian.EventBus;
using YiDian.EventBus.Abstractions;
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
using YiDian.EventBus.Abstractions;

namespace YiDian.EventBusMQ
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        const string BROKER_NAME = "ml_trade_event_bus";
        const string AUTOFAC_SCOPE_NAME = "ml_trade_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly int _retryCount;

        private IModel _consumerChannel;
        IModel _pubChannel;
        private string _queueName;
        private bool _autoAck;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        readonly IQpsCounter _counter;

        public event Action<AggregateException> ProcessException;

        public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection, ILogger logger, ILifetimeScope autofac, IEventBusSubscriptionsManager subsManager, IQpsCounter counter, int retryCount = 5)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _counter = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            hanlerCacheMgr = new EventHanlerCacheMgr(100, autofac, AUTOFAC_SCOPE_NAME);
            _retryCount = retryCount;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
            CreatePublishChannel();
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

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);
            }
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
                _pubChannel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct", durable: true, autoDelete: false);
                _pubChannel.CallbackException += (sender, ea) =>
                {
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }
        public void Publish<T>(T @event) where T : IntegrationMQEvent
        {
            ThreadChannels<IntegrationMQEvent>.Current.QueueWorkItemInternal((e) =>
            {
                var policy = Policy.Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning($"Publish error {ex.Message} , now  try publish again");
                    });
                var eventName = e.GetType().FullName.ToLower();
                var message = JsonConvert.SerializeObject(e);
                var body = Encoding.UTF8.GetBytes(message);
                policy.Execute(() =>
                {
                    _pubChannel.BasicPublish(exchange: BROKER_NAME, routingKey: eventName, basicProperties: null, body: body);
                });
            }, @event);
        }
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
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
            where TH : IDynamicIntegrationEventHandler
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

        private void CreateConsumerChannel(bool isInit, ushort fetchcount, int queueLength, bool autodel, bool durable)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var dic = new Dictionary<string, object>
            {
                //消费队列最大消息数量
                ["x-max-length"] = queueLength
            };
            var channel = _persistentConnection.CreateModel();
            channel.QueueDeclare(queue: _queueName,
                                 durable: durable,
                                 exclusive: false,
                                 autoDelete: autodel,
                                 arguments: dic);

            channel.BasicQos(0, fetchcount, false);
            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = null;
                CreateConsumerChannel(false, fetchcount, queueLength, autodel, durable);
            };
            _consumerChannel = channel;
            if (!isInit) BeginConsumer();
        }

        private void BeginConsumer()
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, ea) =>
            {
                ThreadChannels<BasicDeliverEventArgs>.Current.QueueWorkItemInternal((e) =>
                {
                    ProcessEvent(e);
                }, ea);
            };
            _consumerChannel.BasicConsume(queue: _queueName, autoAck: _autoAck, consumer: consumer);
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
                    hanlerCacheMgr.GetDynamicHandler(subscription.HandlerType, out IDynamicIntegrationEventHandler handler, out ILifetimeScope scope);
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
        public void StartConsumer(string queueName, Action<IEventBus> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                _autoAck = autoAck;
                _queueName = queueName.ToLower();
                CreateConsumerChannel(true, fetchCount, queueLength, autodel, durable);
                action.Invoke(this);
                BeginConsumer();
            }
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
