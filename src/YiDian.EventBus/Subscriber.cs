using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    public class TopicSubscriber : Subscriber<ITopicEventBus>
    {
        readonly string __name;
        public TopicSubscriber(ITopicEventBus eventBus, string name) : base(eventBus)
        {
            __name = name;
        }
        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>(__name, where);
        }
        public void Subscribe<T, TH>()
          where T : IntegrationMQEvent
          where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>(__name);
        }
        public void Subscribe<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            _eventBus.Subscribe<TH>(__name, eventName);
        }
    }
    public class DirectSubscriber : Subscriber<IDirectEventBus>
    {
        readonly string __name;
        public DirectSubscriber(IDirectEventBus eventBus, string name) : base(eventBus)
        {
            __name = name;
        }
        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            _eventBus.Subscribe<T, TH>(__name);
        }
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            _eventBus.Subscribe<TH>(__name, eventName);
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
