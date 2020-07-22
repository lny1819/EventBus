using Microsoft.Extensions.Logging;
using System;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// Direct 类型 消息总线实现
    /// </summary>
    public class DirectEventBus : EventBusBase<IDirectEventBus, DirectSubscriber>, IDirectEventBus
    {
        readonly string brokerName = "amq.direct";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="autofac"></param>
        /// <param name="persistentConnection"></param>
        /// <param name="seralize"></param>
        /// <param name="retryCount"></param>
        /// <param name="cacheCount"></param>
        public DirectEventBus(ILogger<IDirectEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int retryCount = 5, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, retryCount, cacheCount)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="brokerName"></param>
        /// <param name="logger"></param>
        /// <param name="autofac"></param>
        /// <param name="persistentConnection"></param>
        /// <param name="seralize"></param>
        /// <param name="retryCount"></param>
        /// <param name="cacheCount"></param>
        public DirectEventBus(string brokerName, ILogger<IDirectEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int retryCount = 5, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, retryCount, cacheCount)
        {
            if (string.IsNullOrEmpty(brokerName)) throw new ArgumentNullException(nameof(brokerName), "broker name can not be null");
            this.brokerName = brokerName;
            persistentConnection.TryConnect();
            var channel = persistentConnection.CreateModel();
            channel.ExchangeDeclare(brokerName, "topic", false, false, null);
            channel.Dispose();
        }
        /// <summary>
        /// Exchange名称
        /// </summary>
        public override string BROKER_NAME => brokerName;
        /// <summary>
        /// 获取路由键
        /// </summary>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public override string GetEventKeyFromRoutingKey(string routingKey)
        {
            return routingKey;
        }

        public override bool Publish<T>(T @event, out ulong tag, bool enableTransaction = false)
        {
            return Publish(@event, (x) => x, out _, out tag, enableTransaction);
        }

        public override void Subscribe<T, TH>(string queueName)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var subkey = mgr.GetEventKey<T>();
                    mgr.AddSubscription<T, TH>(subkey, BROKER_NAME);
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

        public void SubscribeBytes<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var subkey = mgr.GetEventKey<T>();
                    mgr.AddBytesSubscription<T, TH>(subkey, BROKER_NAME);
                    break;
                }
            }
        }
        public void UnsubscribeBytes<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    mgr.RemoveBytesSubscription<T, TH>();
                    break;
                }
            }
        }
        public void RegisterConsumer(string queueName, Action<DirectSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck, bool autoStart)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new DirectSubscriber(this, queueName);
            var submgr = GetSubscriber(queueName);
            config = new ConsumerConfig<IDirectEventBus, DirectSubscriber>(scriber, submgr)
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
            CreateConsumerChannel(config, autoStart);
        }
    }
}
