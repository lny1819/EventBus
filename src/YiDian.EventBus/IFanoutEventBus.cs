using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    public interface IFanoutEventBus : IEventBus
    {
        void RegisterConsumer(string queuename, Action<FanoutSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = true, bool durable = false, bool autoAck = true, bool autoStart = true);
    }
}
