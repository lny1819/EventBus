using ML.MqLogger.MQLogsEventBus.Abstractions;
using System;
using ML.MqLogger.Abstractions;
using ML.MqLogger.Logmodel;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Polly.Retry;
using System.Net.Sockets;
using Polly;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System.Linq;
using ML.EventBusMQ;
using System.Collections.Generic;
using ML.EventBus;
using Autofac;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ML.MqLogger.MQLogsEventBus
{

    public class LogEventBusMQ : ILogEventBus, IDisposable
    {
        const string BROKER_NAME = "ml_log_event_bus";
        const string AUTOFAC_SCOPE_NAME = "ml_log_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ConsoleLog _logger;
        private readonly ILogEventBusSubMgr _subsManager;
        private readonly int _retryCount;
        ILifetimeScope _autofac;
        private IModel _consumerChannel;
        IModel _pubChannel;
        private string _queueName;

        public event Action<AggregateException> ProcessException;

        public LogEventBusMQ(IRabbitMQPersistentConnection persistentConnection, ILifetimeScope autofac, LogEventBusMQSubManager subsManager = null, int retryCount = 5)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _subsManager = subsManager ?? new LogEventBusMQSubManager();
            _autofac = autofac ?? throw new ArgumentNullException(nameof(ILifetimeScope));
            _retryCount = retryCount;
            _logger = new ConsoleLog();
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
            CreatePublishChannel();
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
                _pubChannel = _persistentConnection.CreateModel();
                _pubChannel.ExchangeDeclare(exchange: BROKER_NAME, type: "topic", durable: true, autoDelete: false);
                _pubChannel.CallbackException += (sender, ea) =>
                {
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }

        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : ILogEventHandler<T>
        {
            DoInternalSubscription(typeof(T).FullName);
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
            where TH : ILogEventHandler<T>
            where T : IntegrationMQEvent
        {
            _subsManager.RemoveSubscription<T, TH>();
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
            consumer.Received += (model, ea) => ProcessEvent(ea);
            _consumerChannel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        }

        private async void ProcessEvent(BasicDeliverEventArgs ea)
        {
            var eventName = ea.RoutingKey;
            var handlers = _subsManager.GetHandlersForEvent(eventName, out Type eventType);
            var length = handlers.Count();
            if (length == 0) return;
            var message = Encoding.UTF8.GetString(ea.Body);
            var type = typeof(LoggerEvent<>).MakeGenericType(new Type[] { eventType });
            var gettersetter = EntitysMetas.GetTypeGetterSetter(type);
            var integrationEvent = JsonConvert.DeserializeObject(message, type);
            var item = gettersetter["Item"].Item1(integrationEvent);
            var errmsg = gettersetter["ErrMsg"].Item1(integrationEvent);
            var datetime = gettersetter["DateTime"].Item1(integrationEvent);
            var tasks = new List<Task<bool>>(length);
            var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME);
            foreach (var subscription in handlers)
            {
                var handler = scope.Resolve(subscription.SubType) as ILogEventHandler;
                var t = (Task)subscription.Method(handler, new object[] { item, datetime, errmsg });
                tasks.Add(t.ContinueWith(e =>
                {
                    var flag = ProcessExceptionHandler(e);
                    return flag;
                }));
            }
            await Task.Factory.ContinueWhenAll(tasks.ToArray(), e =>
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
                if (ProcessException == null) _logger.LogError(task.Exception.ToString());
                else ProcessException(task.Exception);
                return false;
            }
            return true;
        }
        public void StartConsumer(string queueName, Action<ILogEventBus> action, ushort fetchCount, int queueLength = 100000, bool autodel = false, bool durable = true)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                _queueName = queueName.ToLower();
                CreateConsumerChannel(true, fetchCount, queueLength, autodel, durable);
                action.Invoke(this);
                BeginConsumer();
            }
        }
        public void Publish(LoggerEvent @event)
        {
            Task.Factory.StartNew((o) =>
            {
                var policy = Policy.Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning($"Publish error {ex.Message} , now  try publish again");
                    });
                var eventName = @event.Item.GetType().FullName;
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    _pubChannel.BasicPublish(exchange: BROKER_NAME, routingKey: eventName, basicProperties: null, body: body);
                });
            }, null);
        }
    }
}
