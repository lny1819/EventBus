using System;

namespace YiDian.EventBus.Abstractions
{
    public class SubKeyHandler
    {
        IEventBusSubscriptionsManager _mgr;
        Action<string, string> _doMqSub;
        ITopicEventBus _topic;
        public SubKeyHandler(IEventBusSubscriptionsManager mgr, Action<string, string> doMqSub, ITopicEventBus topic)
        {
            _doMqSub = doMqSub;
            _mgr = mgr;
            _topic = topic;
        }
       
    }
}
