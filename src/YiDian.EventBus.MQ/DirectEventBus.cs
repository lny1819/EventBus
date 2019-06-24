using Autofac;
using Microsoft.Extensions.Logging;
using System;

namespace YiDian.EventBus.MQ
{
    public class DirectEventBus : EventBusBase<IDirectEventBus, DirectSubscriber>, IDirectEventBus
    {
        public DirectEventBus(ILogger<IDirectEventBus> logger, ILifetimeScope autofac, IRabbitMQPersistentConnection persistentConnection = null, IEventBusSubscriptionsManagerFactory factory = null, IEventSeralize seralize = null, int retryCount = 5, int cacheCount = 100) : base(logger, autofac, persistentConnection, factory, seralize, retryCount, cacheCount)
        {
        }

        public override string BROKER_NAME => "amq.direct";

        public override string AUTOFAC_SCOPE_NAME => "DirectEventBus";

        public override string GetEventKey(string routingKey)
        {
            return routingKey;
        }

        public override void Publish<T>(T @event, bool enableTransaction = false)
        {
            var name = GetSubscriber("publish").GetEventKey<T>();
            Publish(@event, name, enableTransaction);
        }

        public void StartConsumer(string queueName, Action<DirectSubscriber> action, ushort fetchCount, int queueLength, bool autodel, bool durable, bool autoAck)
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
            CreateConsumerChannel(config);
        }
    }
}
