using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace YiDian.EventBus.MQ
{
    public class ConsumerConfig<TEventBus, TSub>
        where TEventBus : IEventBus
        where TSub : Subscriber<TEventBus>
    {
        private readonly TSub _subscriber;
        private readonly IEventBusSubManager __manager;
        private IModel _model;
        EventingBasicConsumer _consumer;

        public ConsumerConfig(TSub subscriber, IEventBusSubManager mgr)
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


        internal void Register(IModel channel, Action<ConsumerConfig<TEventBus, TSub>, BasicDeliverEventArgs> config_OnReceive, bool autoStart)
        {
            var old = _model;
            _model = channel;
            if (old != null && old != channel)
                old.Dispose();
            if (old == _model) return;
            _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += (model, ea) =>
            {
                config_OnReceive(this, ea);
            };
            SubAction.Invoke(_subscriber);
            if (autoStart) Start();
        }
        internal void Start()
        {
            _model.BasicConsume(Name, AutoAck, _consumer);
        }
        internal IModel GetChannel()
        {
            var old = _model;
            return old;
        }

        internal IEventBusSubManager GetSubMgr()
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
