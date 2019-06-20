using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    public class TopicSubscriber : Subscriber<ITopicEventBus>
    {
        public TopicSubscriber(ITopicEventBus eventBus, string name) : base(eventBus, name)
        {
        }

        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, where);
        }
        public void Subscribe<T, TH>()
          where T : IntegrationMQEvent
          where TH : IIntegrationEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        public void Subscribe<T, TH>(string eventName)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, eventName);
        }
        public void Subscribe<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            __eventBus.Subscribe<TH>(__name, eventName);
        }
    }
    public class DirectSubscriber : Subscriber<IDirectEventBus>
    {
        public DirectSubscriber(IDirectEventBus eventBus, string name) : base(eventBus, name)
        {
        }

        public void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            __eventBus.Subscribe<TH>(__name, eventName);
        }
    }
    public abstract class Subscriber<TEventBus> where TEventBus : IEventBus
    {
        readonly protected TEventBus __eventBus;
        readonly protected string __name;
        public Subscriber(TEventBus eventBus, string name)
        {
            __name = name;
            __eventBus = eventBus;
        }
    }
}
