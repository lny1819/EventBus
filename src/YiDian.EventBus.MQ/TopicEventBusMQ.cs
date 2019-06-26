using Autofac;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{

    public class TopicEventBusMQ : EventBusBase<ITopicEventBus, TopicSubscriber>, ITopicEventBus
    {
        public TopicEventBusMQ(ILogger<ITopicEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection, ILogger<IEventBusSubManager> sub_logger, IEventBusSubManagerFactory factory = null, IEventSeralize seralize = null, int retryCount = 5, int cacheCount = 100) : base(logger, autofac, persistentConnection, sub_logger, factory, seralize, retryCount, cacheCount)
        {
        }

        public override string BROKER_NAME => "amq.topic";

        public override string AUTOFAC_SCOPE_NAME => "TopicEventBus";

        public override void Publish<T>(T @event, bool enableTransaction = false)
        {
            var fix = GetPubKey(@event);
            Publish(@event, (x) => fix + x, enableTransaction);
        }
        public void PublishPrefix<T>(T @event, string prefix, bool enableTransaction = false) where T : IMQEvent
        {
            var pubkey = GetPubKey<T>(@event);
            Publish(@event, (x) => prefix + "." + pubkey + x, enableTransaction);
        }
        public override string GetEventKeyFromRoutingKey(string routingKey)
        {
            var index = routingKey.LastIndexOf('.');
            if (index == -1)
                return routingKey;
            return routingKey.Substring(index + 1);
        }
        string GetPubKey<T>(T @event) where T : IMQEvent
        {
            var type = typeof(T);
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null || props.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            var values = props.Values.ToList().OrderBy(e => e.Index);
            foreach (var p in values)
            {
                var value = p.Property(@event);
                if (value.GetType().IsValueType) value = ((int)value).ToString();
                sb.Append(value.ToString());
                sb.Append('.');
            }
            var key = sb.ToString();
            return key;
        }
        string GetSubKey<T>() where T : IMQEvent
        {
            var type = typeof(T);
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null || props.Count == 0) return string.Empty;
            return "#.";
        }
        string GetSubKey<T>(Expression<Func<T, bool>> where) where T : IMQEvent
        {
            var type = typeof(T);
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return string.Empty;
            var body = where.Body as BinaryExpression;
            var dic = new Dictionary<string, string>();
            GetMembers(body, dic);
            var sb = new StringBuilder();
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
            return sb.ToString();
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
             where T : IMQEvent
             where TH : IEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var subkey = GetSubKey(where);
                    mgr.AddSubscription<T, TH>(subkey);
                    break;
                }
            }
        }

        public void Unsubscribe<T>(string queueName, Expression<Func<T, bool>> where)
            where T : IMQEvent
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var subkey = GetSubKey(where);
                    mgr.RemoveSubscription(subkey);
                    break;
                }
            }
        }

        public void Subscribe<T, TH>(string queueName, string subkey)
              where T : IMQEvent
             where TH : IEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.AddSubscription<T, TH>(subkey);
                    break;
                }
            }
        }
        public void Unsubscribe(string queueName, string subkey)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.RemoveSubscription(subkey);
                    break;
                }
            }
        }
        public void SubscribeBytes<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeBytes<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            throw new NotImplementedException();
        }

        public override void Subscribe<T, TH>(string queueName)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventKey = mgr.GetEventKey<T>();
                    var subkey = GetSubKey<T>() + eventKey;
                    mgr.AddSubscription<T, TH>(subkey);
                    break;
                }
            }
        }

        public override void Unsubscribe<T, TH>(string queueName)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.RemoveSubscription<T, TH>();
                    break;
                }
            }
        }
    }
}