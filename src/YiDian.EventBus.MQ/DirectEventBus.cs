using Autofac;
using Microsoft.Extensions.Logging;
using System;

namespace YiDian.EventBus.MQ
{
    public class DirectEventBus : EventBusBase<IDirectEventBus, DirectSubscriber>, IDirectEventBus
    {
        public DirectEventBus(ILogger<IDirectEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection = null, int retryCount = 5, int cacheCount = 100) : base(logger, autofac, persistentConnection, retryCount, cacheCount)
        {
        }

        public override string BROKER_NAME => "amq.direct";

        public override string GetEventKeyFromRoutingKey(string routingKey)
        {
            return routingKey;
        }

        public override void Publish<T>(T @event, bool enableTransaction = false)
        {
            Publish(@event, (x) => x, enableTransaction);
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
                    mgr.AddBytesSubscription<T, TH>(subkey,BROKER_NAME);
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
