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
        public TopicEventBusMQ(ILogger<ITopicEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection = null, IEventBusSubscriptionsManagerFactory factory = null, ISeralize seralize = null, int retryCount = 5, int cacheCount = 100) : base(logger, autofac, persistentConnection, factory, seralize, retryCount, cacheCount)
        {
        }

        public override string BROKER_NAME => "amq.topic";

        public override string AUTOFAC_SCOPE_NAME => "TopicEventBus";

        public override void Publish<T>(T @event, bool enableTransaction = false)
        {
            var name = GetPubKey(@event);
            base.Publish(@event, name, enableTransaction);
        }
        public override string GetEventKey(string routingKey)
        {
            if (routingKey.IndexOf('~') > -1) routingKey = routingKey.Substring(0, routingKey.IndexOf('~') - 1) + ".#";
            //else if (routingKey.IndexOf('-') > -1)
            //{

            //}
            return routingKey;
        }

        string GetPubKey<T>(T @event) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var eventKey = GetSubscriber("publish").GetEventKey<T>();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return eventKey;
            var sb = new StringBuilder();
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
        string GetSubKey<T>(Expression<Func<T, bool>> where, IEventBusSubscriptionsManager mgr) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            var fullname = mgr.GetEventKey<T>();
            var dic = new Dictionary<string, string>();
            var props = TypeEventBusMetas.GetKeys(type, out string keyname);
            if (props == null) return fullname;
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
        public override void Publish<T>(T @event, string prefix, bool enableTransaction = false)
        {
            if (string.IsNullOrEmpty(prefix)) Publish(@event, enableTransaction);
            else
            {
                var name = GetSubscriber("publish").GetEventKey<T>();
                var sb = new StringBuilder(prefix);
                sb.Append('.');
                sb.Append('~');
                sb.Append('.');
                sb.Append(name);
                name = sb.ToString();
                base.Publish(@event, name, enableTransaction);
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
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var keyname = GetSubKey(where, mgr);
                    mgr.AddSubscription<T, TH>(keyname);
                    break;
                }
            }
        }

        public void Unsubscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var keyname = GetSubKey(where, mgr);
                    item.Unsubscribe<T, TH>(keyname);
                    break;
                }
            }
        }

        public void Subscribe<T, TH>(string queueName, string prifix)
              where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventName = prifix + ".#";
                    mgr.AddSubscription<T, TH>(eventName);
                    break;
                }
            }
        }
        public void Unsubscribe<T, TH>(string queueName, string prifix)
              where T : IntegrationMQEvent
              where TH : IIntegrationEventHandler<T>
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventName = prifix + ".#";
                    mgr.RemoveSubscription<T, TH>(eventName);
                    break;
                }
            }
        }

    }
}