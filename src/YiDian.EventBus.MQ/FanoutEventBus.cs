using Microsoft.Extensions.Logging;
using System;

namespace YiDian.EventBus.MQ
{
    internal class FanoutEventBus : EventBusBase<IFanoutEventBus, FanoutSubscriber>, IFanoutEventBus
    {
        readonly string brokerName = "amq.fanout";
        public FanoutEventBus(ILogger<IFanoutEventBus> logger, IServiceProvider autofac, IEventSeralize seralize, IRabbitMQPersistentConnection persistentConnection, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, cacheCount)
        {

        }
        public FanoutEventBus(string brokerName, ILogger<IFanoutEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int cacheCount = 100)
          : base(logger, autofac, seralize, persistentConnection, cacheCount)
        {
            this.brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName), "broker name can not be null");
            persistentConnection.TryConnect();
            var channel = persistentConnection.CreateModel();
            channel.ExchangeDeclare(brokerName, "fanout", false, true, null);
            channel.Dispose();
        }
        public override string BROKER_NAME => brokerName;

        public override string GetEventKeyFromRoutingKey(string routingKey)
        {
            return routingKey;
        }

        public override bool Publish<T>(T @event, out ulong tag, bool enableTransaction = false)
        {
            return Publish(@event, (x) => x, out _, out tag, false);
        }

        public void RegisterConsumer(string queueName, Action<FanoutSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = true, bool durable = false, bool autoAck = true, bool autoStart = true)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new FanoutSubscriber(this, queueName);
            var submgr = GetSubscriber(queueName);
            config = new ConsumerConfig<IFanoutEventBus, FanoutSubscriber>(scriber, submgr)
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

        public override void Subscribe<T, TH>(string queueName)
        {
            foreach (var item in consumerInfos)
            {
                if (item.Name == queueName)
                {
                    var mgr = item.GetSubMgr();
                    var eventKey = mgr.GetEventKey<T>();
                    mgr.AddSubscription<T, TH>(eventKey, BROKER_NAME);
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