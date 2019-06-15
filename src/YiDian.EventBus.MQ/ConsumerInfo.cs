using System;
using System.Collections.Concurrent;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace YiDian.EventBus.MQ
{
    internal class ConsumerConfig<TEventBus, TSub>
        where TEventBus : IEventBus
        where TSub : Subscriber<TEventBus>
    {
        private readonly ConcurrentQueue<BasicDeliverEventArgs> _queue;
        private readonly TSub _subscriber;
        private IModel _model;

        public ConsumerConfig(TSub subscriber, ConcurrentQueue<BasicDeliverEventArgs> queue)
        {
            _subscriber = subscriber;
            _queue = queue;
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
                _model.Dispose();
            if (old == _model) return;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                _queue.Enqueue(ea);
                CheckQueue();
            };
            SubAction.Invoke(_subscriber);
            _model.BasicConsume(queue: Name, autoAck: AutoAck, consumer: consumer);
        }

        private void CheckQueue()
        {
            if (_queue.Count > 10000)
                Thread.Sleep(30);
            else if (_queue.Count > 20000)
            {
                Thread.Sleep(1000);
            }
            else if (_queue.Count > 30000)
            {
                Thread.Sleep(5000);
            }
        }
    }
}
