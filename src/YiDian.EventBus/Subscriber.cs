using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    public class TopicSubscriber : Subscriber<ITopicEventBus>
    {
        public TopicSubscriber(ITopicEventBus eventBus) : base(eventBus)
        {
        }
        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>(where);
        }
    }
    public class DirectSubscriber : Subscriber<IEventBus>
    {
        public DirectSubscriber(IEventBus eventBus) : base(eventBus)
        {
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
    public abstract class Subscriber<TEventBus> where TEventBus : IEventBus
    {
        readonly protected TEventBus _eventBus;
        public Subscriber(TEventBus eventBus)
        {
            _eventBus = eventBus;
        }
    }
}
