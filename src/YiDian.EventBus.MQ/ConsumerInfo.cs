using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace YiDian.EventBus.MQ
{
    internal class ConsumerConfig<TEventBus, TSub>
        where TEventBus : IEventBus
        where TSub : Subscriber<TEventBus>
    {
        private readonly TSub _subscriber;
        private readonly IEventBusSubscriptionsManager __manager;
        private IModel _model;

        public event EventHandler<BasicDeliverEventArgs> OnReceive;

        public ConsumerConfig(TSub subscriber, IEventBusSubscriptionsManager mgr)
        {
            __manager = mgr;
            _subscriber = subscriber;
        }
        public string Name { get; internal set; }
        public bool AutoAck { get; internal set; }
        public int MaxLength { get; internal set; }
        public bool Durable { get; internal set; }
        public bool AutoDel { get; internal set; }
        public ushort FetchCount { get; internal set; }
        public Action<TSub> SubAction { get; internal set; }


        internal void Start(IModel channel)
        {
            var old = _model;
            _model = channel;
            if (old != null && old != channel)
                old.Dispose();
            if (old == _model) return;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                OnReceive(this, ea);
            };
            SubAction.Invoke(_subscriber);
            _model.BasicConsume(queue: Name, autoAck: AutoAck, consumer: consumer);
        }
        internal IModel GetChannel()
        {
            var old = _model;
            return old;
        }
        internal void Unsubscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            __manager.RemoveSubscription<T, TH>();
        }
        internal void Unsubscribe<T, TH>(string eventName)
           where T : IntegrationMQEvent
           where TH : IIntegrationEventHandler<T>
        {
            __manager.RemoveSubscription<T, TH>(eventName);
        }
        internal void Unsubscribe<TH>(string eventName) where TH : IDynamicBytesHandler
        {
            __manager.RemoveSubscription<TH>(eventName);
        }

        internal IEventBusSubscriptionsManager GetSubMgr()
        {
            return __manager;
        }

        internal void Dispose()
        {
            var channel = GetChannel();
            _model = null;
            if (channel == null) return;
            channel.Dispose();
        }
    }



    struct QueueItem<TEventBus, TSub>
        where TEventBus : IEventBus
        where TSub : Subscriber<TEventBus>
    {

        public QueueItem(ConsumerConfig<TEventBus, TSub> config, BasicDeliverEventArgs e) : this()
        {
            this.ConsumerConfig = config;
            this.Event = e;
        }

        public ConsumerConfig<TEventBus, TSub> ConsumerConfig { get; }
        public BasicDeliverEventArgs Event { get; }
    }
}
