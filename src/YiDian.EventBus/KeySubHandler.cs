using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    public class KeySubHandler
    {
        readonly ITopicEventBus _eventBus;
        public KeySubHandler(ITopicEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>(where);
        }

        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>();
        }
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            _eventBus.SubscribeDynamic<TH>(eventName);
        }
    }
}
