using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace YiDian.EventBus.MQ
{
    internal class ConsumerConfig
    {
        public IModel Channel { get; private set; }
        public string Name { get; internal set; }
        public bool AutoAck { get; internal set; }

        internal bool TryOpen()
        {
            throw new NotImplementedException();
        }
    }
}
