using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{

    public class TopicEventBusMQ : ITopicEventBus, IDisposable
    {

        const string BROKER_NAME = "amq.topic";
        const string AUTOFAC_SCOPE_NAME = "TopicEventBus";

        const int ProcessStop = 0;
        const int ProcessStart = 1;

        private int process_state = ProcessStop;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManagerFactory _subsFactory;
        private readonly EventHanlerCacheMgr hanlerCacheMgr;
        IModel _pubChannel;
        readonly int _retryCount;
        readonly ILogger<IEventBus> _logger;
        readonly List<ConsumerConfig<ITopicEventBus, TopicSubscriber>> consumerInfos;
        readonly ConcurrentQueue<QueueItem<IDirectEventBus, DirectSubscriber>> __processQueue;

        public TopicEventBusMQ(IRabbitMQPersistentConnection persistentConnection, ILogger<IEventBus> logger, ILifetimeScope autofac, IEventBusSubscriptionsManagerFactory factory, int retryCount = 5, int cacheCount = 100)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            _persistentConnection.OnConnectRecovery += _persistentConnection_OnConnectRecovery;
            _logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            __processQueue = new ConcurrentQueue<QueueItem<IDirectEventBus, DirectSubscriber>>();
            _subsFactory = factory ?? new InMemorySubFactory();
            consumerInfos = new List<ConsumerConfig<ITopicEventBus, TopicSubscriber>>();
            hanlerCacheMgr = new EventHanlerCacheMgr(cacheCount, autofac, AUTOFAC_SCOPE_NAME);
            _retryCount = retryCount;
            CreatePublishChannel();
        }
        private void _persistentConnection_OnConnectRecovery(object sender, EventArgs e)
        {
            //foreach (var consumerinfo in consumerInfos)
            //{
            //    CreateConsumerChannel(consumerinfo);
            //}
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

        #region Sub And UnSub
        private void DoInternalSubscription(IEventBusSubscriptionsManager mgr, string eventName)
        {
            var containsKey = mgr.HasSubscriptionsForEvent(eventName);
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
                                      routingKey: eventName.ToLower());
                }
            }
        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            var mgr = (IEventBusSubscriptionsManager)sender;
            var queueName = mgr.QueueName;
            DoInternalUnSub(queueName, eventName);
        }
        private void DoInternalUnSub(string queueName, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName.ToLower());
            }
        }
        #endregion

        string GetPubKey<T>(T @event, string prefix = "") where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var eventKey = _subsFactory.GetOrCreateByQueue("publish").GetEventKey<T>();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null && string.IsNullOrEmpty(prefix)) return eventKey;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                sb.Append(prefix);
                sb.Append('.');
            }
            if (!string.IsNullOrEmpty(keyname))
            {
                sb.Append(keyname);
                sb.Append('.');
            }
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
            sb.Append(eventKey);
            var key = sb.ToString();
            return key;
        }
        string GetSubKey<T>() where T : IntegrationMQEvent
        {
            var eventKey = _subsFactory.GetOrCreateByQueue("publish").GetEventKey<T>();
            var sb = new StringBuilder("#.");
            sb.Append(eventKey);
            var key = sb.ToString();
            return key;
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
        public void Publish<T>(T @event, bool enableTransaction = false) where T : IntegrationMQEvent
        {
            Publish(@event, "", enableTransaction);
        }
        public void Publish<T>(T @event, string prefix, bool enableTransaction = false) where T : IntegrationMQEvent
        {
            var eventName = GetPubKey(@event, prefix);
            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            _pubChannel.BasicPublish(exchange: BROKER_NAME,
                             routingKey: eventName,
                             basicProperties: null,
                             body: body);
        }
        private void Config_OnReceive(object sender, BasicDeliverEventArgs e)
        {
            var config = (ConsumerConfig<IDirectEventBus, DirectSubscriber>)sender;
            __processQueue.Enqueue(new QueueItem<IDirectEventBus, DirectSubscriber>(config, e));
            StartProcess();
            CheckQueue();
        }
        private void CreateConsumerChannel(ConsumerConfig<ITopicEventBus, TopicSubscriber> config)
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
        private void CheckQueue()
        {
            if (__processQueue.Count > 10000)
                Thread.Sleep(30);
            else if (__processQueue.Count > 20000)
            {
                Thread.Sleep(1000);
            }
            else if (__processQueue.Count > 30000)
            {
                Thread.Sleep(5000);
            }
        }
        private void StartProcess()
        {
            if (Interlocked.CompareExchange(ref process_state, ProcessStart, ProcessStop) != ProcessStop) return;
            Task.Run(() =>
            {
                for (; ; )
                {
                    var flag = __processQueue.TryDequeue(out QueueItem<IDirectEventBus, DirectSubscriber> item);
                    if (!flag)
                    {
                        Interlocked.Exchange(ref process_state, ProcessStop);
                        break;
                    }
                    var eventName = item.Event.RoutingKey;
                    var consumer = item.ConsumerConfig;
                    var mgr = consumer.GetSubMgr();
                    var handlers = mgr.GetHandlersForEvent(eventName);
                    if (handlers == null) continue;
                    ProcessEvent(handlers, item);
                }
            });
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
                    await handler.Handle(ea.RoutingKey, ea.Body)
                       .ContinueWith(x =>
                       {
                           hanlerCacheMgr.ResteDymaicHandler(handler, subinfo.HandlerType, scope);
                           if (x.Status == TaskStatus.Faulted) _logger.LogError(x.Exception.ToString());
                           if (!config.AutoAck)
                           {
                               if (x.IsCompletedSuccessfully && x.Result) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                               else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                           }
                       })
                       .ConfigureAwait(false);
                }
                else
                {
                    var integrationEvent = DeserializeObject(ea.Body, subinfo.EventType);
                    hanlerCacheMgr.GetIIntegrationEventHandler(subinfo.HandlerType, out IIntegrationEventHandler handler, out ILifetimeScope scope);
                    var task = (Task<bool>)subinfo.Handler(handler, new object[] { integrationEvent });
                    await task.ContinueWith(x =>
                    {
                        hanlerCacheMgr.ResteTypeHandler(handler, subinfo.HandlerType, scope);
                        if (x.Status == TaskStatus.Faulted) _logger.LogError(x.Exception.ToString());
                        if (!config.AutoAck)
                        {
                            if (x.IsCompletedSuccessfully && x.Result) config.GetChannel().BasicAck(ea.DeliveryTag, false);
                            else config.GetChannel().BasicNack(ea.DeliveryTag, false, true);
                        }
                    })
                    .ConfigureAwait(false);
                }
            }
        }
        private object DeserializeObject(byte[] body, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(body), type);
        }
        public void StartConsumer(string queueName, Action<TopicSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new TopicSubscriber(this, queueName);
            var submgr = _subsFactory.GetOrCreateByQueue(queueName);
            submgr.OnEventRemoved += SubsManager_OnEventRemoved;
            config = new ConsumerConfig<ITopicEventBus, TopicSubscriber>(scriber, submgr)
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
        public void Subscribe<T, TH>(string queueName)
                    where T : IntegrationMQEvent
                   where TH : IIntegrationEventHandler<T>
        {
            var keyname = GetSubKey<T>();
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.AddSubscription<T, TH>();
                    DoInternalSubscription(mgr, keyname);
                }
            }
        }
        public void Subscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.AddSubscription<T, TH>();
                    var keyname = GetSubKey(where);
                    DoInternalSubscription(mgr, keyname);
                }
            }
        }
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
        public void Unsubscribe<T, TH>(string queueName)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
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

        public void Unsubscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            var keyname = GetSubKey(where);
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    item.Unsubscribe<T, TH>(keyname);
                    break;
                }
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

        public void Subscribe(string queueName, string prifix)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventName = prifix + ".#";
                    DoInternalSubscription(mgr, eventName);
                    break;
                }
            }
        }

        public void Unsubscribe(string queueName, string prifix)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventName = prifix + ".#";
                    DoInternalUnSub(queueName, eventName);
                    break;
                }
            }
        }

        public void Unsubscribe<TH>(string queueName, string eventName) where TH : IDynamicBytesHandler
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

        public void EnableHandlerCache(int cacheLength)
        {
            hanlerCacheMgr.CacheLength = cacheLength;
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