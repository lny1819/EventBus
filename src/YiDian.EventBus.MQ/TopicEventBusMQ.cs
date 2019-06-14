using Autofac;
using Microsoft.Extensions.Logging;
using YiDian.EventBus;
using YiDian.EventBus.Abstractions;
using YiDian.EventBus.Abstractions;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YiDian.EventBusMQ
{

    public class TopicEventBusMQ : ITopicEventBus, IDisposable
    {

        const string BROKER_NAME = "ml_topic_event_bus";
        const string AUTOFAC_SCOPE_NAME = "ml_topic_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger _logger;
        private readonly SubManagerD _subD;
        private readonly SubManagerK _subK;
        private readonly SubManagerA _subA;
        private readonly IQpsCounter _counter;
        private IModel _consumerChannel;
        private ConsumerArgu _consumerArgu;
        Func<string, byte[], bool> pre_handler = null;
        IModel _pubChannel;
        private int _retryCount;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;

        public event Action<Exception> ProcessException;
        /// <summary>
        /// Topic类型EventBus
        /// </summary>
        /// <param name="persistentConnection">MQ连接</param>
        /// <param name="logger">日志</param>
        /// <param name="autofac">autofac 容器</param>
        /// <param name="retryCount">发布重试次数</param>
        public TopicEventBusMQ(IRabbitMQPersistentConnection persistentConnection, ILogger logger, ILifetimeScope autofac, IQpsCounter counter, int retryCount = 5)
        {
            hanlerCacheMgr = new EventHanlerCacheMgr(100, autofac, AUTOFAC_SCOPE_NAME);
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            persistentConnection.OnConnectRecovery += PersistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            _counter = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            _subD = new SubManagerD(DoInternalUnSub, DoInternalSubscription);
            _subK = new SubManagerK(DoInternalUnSub, DoInternalSubscription);
            _subA = new SubManagerA();
            _retryCount = retryCount;
            CreatePublishChannel();
        }

        private void PersistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {

        }

        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
        }
        private void DoInternalSubscription(string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueBind(queue: _consumerArgu.QueueName,
                                  exchange: BROKER_NAME,
                                  routingKey: eventName);
            }
        }
        private void DoInternalUnSub(string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _consumerArgu.QueueName,
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
                    _logger.LogError($"the pubChannel has been shut down");
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }
        string GetPubKey<T>(T @event) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var fullname = type.FullName.ToLower();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return fullname;
            var sb = new StringBuilder(keyname);
            if (!string.IsNullOrEmpty(keyname)) sb.Append('.');
            var values = props.Values.ToList().OrderBy(e => e.Index);
            foreach (var p in values)
            {
                var value = p.Property(@event);
                if (value.GetType().IsValueType) value = ((int)value).ToString();
                sb.Append(value.ToString());
                sb.Append('.');
            }
            sb.Replace('-', '_');
            sb.Append('-');
            sb.Append('.');
            sb.Append(fullname);
            var key = sb.ToString();
            return key.ToLower();
        }
        string GetSubKey<T>() where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var fullname = type.FullName.ToLower();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return fullname;
            var sb = new StringBuilder(keyname);
            if (!string.IsNullOrEmpty(keyname)) sb.Append('.');
            if (props.Count > 0) sb.Append("#.");
            sb.Append(fullname);
            return sb.ToString().ToLower();
        }
        string GetSubKey<T>(Expression<Func<T, bool>> where) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var fullname = type.FullName;
            var dic = new Dictionary<string, string>();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return fullname.ToLower();
            var body = where.Body as BinaryExpression;
            GetMembers(body, dic);
            var sb = new StringBuilder(keyname);
            if (!string.IsNullOrEmpty(keyname)) sb.Append('.');
            var lst = props.OrderBy(e => e.Value.Index).ToList();
            lst.ForEach(e =>
            {
                if (dic.ContainsKey(e.Key))
                {
                    sb.Append(dic[e.Key]);
                    sb.Append('.');
                }
                else sb.Append("*.");
            });
            sb.Append('-');
            sb.Append('.');
            sb.Append(fullname);
            return sb.ToString().ToLower();
        }
        string GetSubKey<T>(Expression<Func<T, bool>> where, string key) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var dic = new Dictionary<string, string>();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return type.FullName.ToLower();
            var body = where.Body as BinaryExpression;
            GetMembers(body, dic);
            var sb = new StringBuilder(key);
            if (!string.IsNullOrEmpty(key)) sb.Append('.');
            var lst = props.OrderBy(e => e.Value.Index).ToList();
            lst.ForEach(e =>
            {
                if (dic.ContainsKey(e.Key))
                {
                    sb.Append(dic[e.Key]);
                    sb.Append('.');
                }
                else sb.Append("*.");
            });
            sb.Append('#');
            return sb.ToString().ToLower();
        }
        void GetMembers(BinaryExpression body, Dictionary<string, string> dic)
        {
            var right = body.Right as BinaryExpression;
            if (!(body.Left is BinaryExpression left))
            {
                if (body.Left is MemberExpression member)
                {
                    var name = member.Member.Name;
                    var value = body.Right.GetParameExpressionValue();
                    if (value != null)
                        dic.Add(name, value.ToString());
                }
                else if (body.Left is UnaryExpression unary)
                {
                    var name = (unary.Operand as MemberExpression).Member.Name;
                    var value = body.Right.GetParameExpressionValue();
                    if (value != null)
                        dic.Add(name, value.ToString());
                }
            }
            else
            {
                GetMembers(left, dic);
                if (right != null) GetMembers(right, dic);
            }
        }
        public void Publish(string key, byte[] datas)
        {
            _pubChannel.BasicPublish(exchange: BROKER_NAME,
                                     routingKey: key,
                                     basicProperties: null,
                                     body: datas);
        }
        public void Publish<T>(T @event) where T : IntegrationMQEvent
        {
            ThreadChannels<T>.Current.QueueWorkItemInternal((o) =>
            {
                var eventName = GetPubKey(o);
                var policy = Policy.Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning($"Publish error {ex.Message},now  try publish again");
                    });

                var message = JsonConvert.SerializeObject(o);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    _pubChannel.BasicPublish(exchange: BROKER_NAME,
                                     routingKey: eventName,
                                     basicProperties: null,
                                     body: body);
                });
            }, @event);
        }
        private string ReplaceKey(string key)
        {
            if (key[key.Length - 1] != '.') key += '.';
            if (key.IndexOf('-') == -1) return key;
            key = key.Replace('-', '_');
            return key;
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

        private void CreateConsumerChannel(bool isInit)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var channel = _persistentConnection.CreateModel();
            var dic = new Dictionary<string, object>
            {
                //消费队列最大消息数量
                ["x-max-length"] = _consumerArgu.QueueMsgLength
            };
            channel.BasicQos(0, _consumerArgu.Fetchout, false);
            channel.QueueDeclare(queue: _consumerArgu.QueueName,
                                 durable: _consumerArgu.Durable,
                                 exclusive: false,
                                 autoDelete: _consumerArgu.AutoDelete,
                                 arguments: dic);
             
            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogError($"the consumer on {_consumerArgu.QueueName} has been shut down");
                _consumerChannel.Dispose();
                _consumerChannel = null;
                CreateConsumerChannel(false);
            };
            channel.ModelShutdown += Channel_ModelShutdown;
            channel.BasicRecoverOk += Channel_BasicRecoverOk;
            _consumerChannel = channel;
            if (!isInit) BeginConsumer();
        }

        private void Channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogInformation("ConsumerChannel_Shutdown");
        }

        private void Channel_BasicRecoverOk(object sender, EventArgs e)
        {
            _logger.LogInformation("ConsumerChannel_BasicRecoverOk");
        }

        private void BeginConsumer()
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, ea) =>
            {
                ThreadChannels<BasicDeliverEventArgs>.Current.QueueWorkItemInternal((o) => ProcessEvent(o), ea);
            };
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning($"Publish error {ex.Message},now  try publish again");
                });
            policy.Execute(() =>
            {
                _consumerChannel.BasicConsume(queue: _consumerArgu.QueueName,
                                     autoAck: _consumerArgu.AutoAck,
                                     consumer: consumer);
            });
        }
        private void ProcessEvent(BasicDeliverEventArgs ea)
        {
            var f = pre_handler?.Invoke(ea.RoutingKey, ea.Body);
            if (f.HasValue && f.Value)
            {
                if (!_consumerArgu.AutoAck) _consumerChannel.BasicAck(ea.DeliveryTag, false);
                return;
            }
            var eventName = ea.RoutingKey;
            if (eventName.IndexOf('-') > -1) eventName = eventName.Substring(eventName.IndexOf('-') + 2);
            var message = Encoding.UTF8.GetString(ea.Body);
            var a = DoSubManagerEvents(eventName, message, _subA);
            var b = DoSubManagerEvents(eventName, message, _subK.SubManager);
            var c = DoSubManagerEvents(eventName, message, _subD.SubManager);
            if (a.Count + b.Count + c.Count == 0)
            {
                _logger.LogInformation($"定义了消费队列，但是部分数据未消费，{eventName} 数据：{message}");
                if (!_consumerArgu.AutoAck) _consumerChannel.BasicAck(ea.DeliveryTag, false);
                return;
            }
            Task.Factory.ContinueWhenAll(a.Concat(b).Concat(c).ToArray(), e =>
            {
                bool flag = false;
                e.ToList().ForEach(x => flag = flag | x.Result);
                if (!_consumerArgu.AutoAck)
                {
                    if (flag) _consumerChannel.BasicAck(ea.DeliveryTag, false);
                    else _consumerChannel.BasicNack(ea.DeliveryTag, false, true);
                }
            });
        }
        List<Task<bool>> DoSubManagerEvents(string eventName, string message, IEventBusSubEventHandler subhandler)
        {
            var lst = subhandler.GetHandlersForEvent(eventName);
            if (!subhandler.HasSubscriptionsForEvent(eventName)) return new List<Task<bool>>();
            object integrationEvent = null;
            var eventType = subhandler.GetEventTypeByName(eventName);
            if (eventType != null && eventType.IsSubclassOf(typeof(IntegrationMQEvent)))
                integrationEvent = JsonConvert.DeserializeObject(message, eventType);
            var tasks = new List<Task<bool>>(lst.Count());
            foreach (var subscription in lst)
            {
                if (subscription.IsDynamic)
                {
                    hanlerCacheMgr.GetDynamicHandler(subscription.HandlerType, out IDynamicIntegrationEventHandler handler, out ILifetimeScope scope);
                    tasks.Add(handler.Handle(message)
                    .ContinueWith(e =>
                    {
                        var haserr = ProcessExceptionHandler(e);
                        hanlerCacheMgr.ResteDymaicHandler(handler, subscription.HandlerType, scope);
                        return haserr;
                    }));
                }
                else
                {
                    hanlerCacheMgr.GetIIntegrationEventHandler(subscription.HandlerType, out IIntegrationEventHandler handler, out ILifetimeScope scope);
                    tasks.Add(((Task)subscription.Handler(handler, new object[] { integrationEvent }))
                    .ContinueWith(e =>
                    {
                        var haserr = ProcessExceptionHandler(e);
                        hanlerCacheMgr.ResteTypeHandler(handler, subscription.HandlerType, scope);
                        return haserr;
                    }));
                }
            }
            return tasks;
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
        public void Subscribe<T, TH>()
                    where T : IntegrationMQEvent
                   where TH : IIntegrationEventHandler<T>
        {
            var keyname = GetSubKey<T>();
            _subD.SubManager.AddSubscription<T, TH>();
            _subD.Sub(keyname, typeof(T).FullName);
        }
        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            var keyname = GetSubKey(where);
            _subK.SubManager.AddSubscription<T, TH>();
            _subK.Sub(keyname, typeof(T).FullName);
        }
        public void Subscribe<TH>(string keyname)
             where TH : IDynamicIntegrationEventHandler
        {
            _subK.SubManager.AddDynamicSubscription<TH>(keyname);
            if (keyname.ToLower() == "ml_topic_all_event")
                _subK.Sub("#", keyname);
            else _subK.Sub(keyname, keyname);
        }
        public void SetSubscribeBytesHandler(Func<string, byte[], bool> pre_handler)
        {
            this.pre_handler = pre_handler;
        }
        public void SubscribeBytes(string key)
        {
            DoInternalSubscription(key);
        }
        public void UnSubscribeBytes(string key)
        {
            DoInternalUnSub(key);
        }
        public void Subscribe<T>(Expression<Func<T, bool>> where, string key, Action<KeySubHandler<T>> addHandler)
            where T : IntegrationMQEvent
        {
            var subkey = GetSubKey(where, key);
            addHandler(new KeySubHandler<T>(subkey, _subA));
            DoInternalSubscription(subkey);
        }
        public void Unsubscribe<T>(Expression<Func<T, bool>> where, string key)
            where T : IntegrationMQEvent
        {
            var subkey = GetSubKey(where, key);
            new KeySubHandler<T>(subkey, _subA).RemoveHandler();
            DoInternalUnSub(subkey);
        }
        public void Unsubscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            _subD.SubManager.RemoveSubscription<T, TH>();
        }

        public void Unsubscribe<T, TH>(Expression<Func<T, bool>> where)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            _subK.SubManager.RemoveSubscription<T, TH>();
        }

        public void Unsubscribe<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subK.SubManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void StartConsumer(string queueName, Action<ITopicEventBus> action, ushort fetchcount, int length, bool autodelete, bool durable, bool autoAck)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            if (string.IsNullOrEmpty(_consumerArgu.QueueName))
            {
                _consumerArgu = new ConsumerArgu()
                {
                    AutoAck = autoAck,
                    AutoDelete = autodelete,
                    Durable = durable,
                    Fetchout = fetchcount,
                    QueueMsgLength = length,
                    QueueName = queueName.ToLower()
                };
                CreateConsumerChannel(true);
                action?.Invoke(this);
                BeginConsumer();
                return;
            }
            if (!string.Equals(queueName, _consumerArgu.QueueName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("has been declare on consumer with name " + _consumerArgu.QueueName);
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

        struct ConsumerArgu
        {
            public string QueueName { get; set; }
            public ushort Fetchout { get; set; }
            public int QueueMsgLength { get; set; }
            public bool AutoDelete { get; set; }
            public bool Durable { get; set; }
            public bool AutoAck { get; set; }
        }
    }
}