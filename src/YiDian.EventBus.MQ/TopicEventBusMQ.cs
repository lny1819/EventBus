using Autofac;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{

    public class TopicEventBusMQ : EventBusBase<ITopicEventBus, TopicSubscriber>, ITopicEventBus
    {
        public TopicEventBusMQ(ILogger<ITopicEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection = null, IEventBusSubscriptionsManagerFactory factory = null, ISeralize seralize = null, int retryCount = 5, int cacheCount = 100) : base(logger, autofac, persistentConnection, factory, seralize, retryCount, cacheCount)
        {
        }

        public override string BROKER_NAME => "amq.topic";

        public override string AUTOFAC_SCOPE_NAME => "TopicEventBus";

        string GetPubKey<T>(T @event, string prefix = "") where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var eventKey = GetSubscriber("publish").GetEventKey<T>();
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
            var eventKey = GetSubscriber("publish").GetEventKey<T>();
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
        public void Publish<T>(T @event, string prefix, bool enableTransaction = false) where T : IntegrationMQEvent
        {
            var eventName = GetPubKey(@event, prefix);
            var message = __seralize.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            PublishBase(eventName, body);
        }
        public void StartConsumer(string queueName, Action<TopicSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new TopicSubscriber(this, queueName);
            var submgr = GetSubscriber(queueName);
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
            consumerInfos.Add(config);
            CreateConsumerChannel(config);
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
    }
}