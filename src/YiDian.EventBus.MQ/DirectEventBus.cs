using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// Direct 类型 消息总线实现
    /// </summary>
    internal class DirectEventBus : EventBusBase<IDirectEventBus, DirectSubscriber>, IDirectEventBus
    {
        readonly string brokerName = "amq.direct";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="autofac"></param>
        /// <param name="persistentConnection"></param>
        /// <param name="seralize"></param>
        /// <param name="cacheCount"></param>
        public DirectEventBus(ILogger<IDirectEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, cacheCount)
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
        /// <param name="cacheCount"></param>
        public DirectEventBus(string brokerName, ILogger<IDirectEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, cacheCount)
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

        public override bool Publish<T>(T @event, out ulong tag, bool enableTransaction = false)
        {
            return Publish(@event, "", out _, out tag, enableTransaction);
        }

        public override void Subscribe<T, TH>(string queueName)
        {
            SubscribeInternal<T, TH>(queueName, "");
        }
        public override void Unsubscribe<T, TH>(string queueName)
        {
            UnsubscribeInternal<T, TH>(queueName, "");
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
        public void Subscribe<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            SubscribeInternal<T, TH>(queueName, subkey);
        }

        public void Unsubscribe<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            UnsubscribeInternal<T, TH>(queueName, subkey);
        }

        public void SubscribeBytes<TH>(string queueName, string subkey) where TH : IBytesHandler
        {
            SubscribeBytesInternal<TH>(queueName, subkey);
        }

        public void UnsubscribeBytes<TH>(string queueName, string subkey) where TH : IBytesHandler
        {
            UnsubscribeBytesInternal<TH>(queueName, subkey);
        }

        protected override IEnumerable<SubscriptionInfo> GetDymaicHandlers(IEventBusSubManager mgr, string key)
        {
            return mgr.GetDymaicHandlersBySubKey(key, BROKER_NAME);
        }
    }
}
