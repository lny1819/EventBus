using Autofac;
using Microsoft.Extensions.Logging;
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

namespace YiDian.EventBus.MQ
{
    public class TopicEventBusBytes : ITopicEventBusBbytes, IDisposable
    {
        const string BROKER_NAME = "ml_topic_event_bus";
        const string AUTOFAC_SCOPE_NAME = "ml_topic_event_bus";
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger _logger;
        private readonly IQpsCounter _counter;
        private IModel _pubChannel;
        private readonly SubManagerD _sub;
        readonly Dictionary<string, ConsumerInfo> _consumers;

        public TopicEventBusBytes(IRabbitMQPersistentConnection persistentConnection, ILogger logger, ILifetimeScope autofac, IQpsCounter counter, int retryCount = 5)
        {
            _consumers = new Dictionary<string, ConsumerInfo>();
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            persistentConnection.OnConnectRecovery += PersistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            _counter = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            _sub = new SubManagerD(DoInternalUnSub, DoInternalSubscription);
            CreatePublishChannel();
        }

        private void DoInternalSubscription(string eventName)
        {
            _pubChannel.QueueBind(queue: _consumerArgu.QueueName,
                              exchange: BROKER_NAME,
                              routingKey: eventName);
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

        private void CreatePublishChannel()
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

        private void PersistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {

        }
        public void Dispose()
        {

        }
        private void ProcessEvent(BasicDeliverEventArgs ea)
        {

        }
        public void Publish(string key, byte[] datas)
        {
            _pubChannel.BasicPublish(exchange: BROKER_NAME,
                                      routingKey: key,
                                      basicProperties: null,
                                      body: datas);
        }

        public void StartConsumer(string queuename, Action<ITopicEventBusBbytes> action, ushort fetchcount = 200, int length = 200000, bool autodelete = false, bool durable = true, bool autoAck = false)
        {
            if (!string.IsNullOrEmpty(queuename) && _consumers.ContainsKey(queuename))
            {
                var info = new ConsumerInfo(_persistentConnection, _logger, ProcessEvent)
                {
                    AutoAck = autoAck,
                    AutoDelete = autodelete,
                    Durable = durable,
                    Fetchout = fetchcount,
                    QueueMsgLength = length,
                    QueueName = queuename.ToLower()
                };
                _consumers.Add(queuename, info);
                action?.Invoke(this);
                info.BeginConsumer();
                return;
            }
        }

        public void Subscribe(string key)
        {

        }

        public void UnSubscribe(string key)
        {

        }

        public void DeleteQueue(string queuename, bool force)
        {

        }

        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            var keyname = GetSubKey<T>();
            _subD.SubManager.AddSubscription<T, TH>();
            _subD.Sub(keyname, typeof(T).FullName);
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
        class ConsumerInfo
        {
            IModel _channel;
            readonly ILogger _logger;
            readonly IRabbitMQPersistentConnection _conn;
            readonly Action<BasicDeliverEventArgs> _consumer;
            public ConsumerInfo(IRabbitMQPersistentConnection conn, ILogger logger, Action<BasicDeliverEventArgs> consumer)
            {
                _consumer = consumer;
                _conn = conn;
                _logger = logger;
            }
            public string QueueName { get; set; }
            public ushort Fetchout { get; set; }
            public int QueueMsgLength { get; set; }
            public bool AutoDelete { get; set; }
            public bool Durable { get; set; }
            public bool AutoAck { get; set; }

            public void BeginConsumer()
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    ThreadChannels<BasicDeliverEventArgs>.Current.QueueWorkItemInternal((o) => _consumer(o), ea);
                };
                var policy = Policy.Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning($"Publish error {ex.Message},now  try publish again");
                    });
                policy.Execute(() =>
                {
                    _channel.BasicConsume(queue: QueueName,
                                         autoAck: AutoAck,
                                         consumer: consumer);
                });
            }
            private void CreateConsumerChannel(bool isInit)
            {
                if (!_conn.IsConnected)
                {
                    _conn.TryConnect();
                }
                var channel = _conn.CreateModel();
                var dic = new Dictionary<string, object>
                {
                    //消费队列最大消息数量
                    ["x-max-length"] = QueueMsgLength
                };
                channel.BasicQos(0, Fetchout, false);
                channel.QueueDeclare(queue: QueueName,
                                     durable: Durable,
                                     exclusive: false,
                                     autoDelete: AutoDelete,
                                     arguments: dic);
                channel.CallbackException += ChannelException;
                channel.BasicRecoverOk += RecoverOk;
                _channel = channel;
                if (!isInit) BeginConsumer();
            }
            internal void ChannelException(object sender, CallbackExceptionEventArgs ea)
            {
                _logger.LogError($"the consumer on {QueueName} has been shut down");
                _channel.Dispose();
                _channel = null;
                CreateConsumerChannel(false);
            }

            internal void RecoverOk(object sender, EventArgs e)
            {

            }
        }
    }

}
