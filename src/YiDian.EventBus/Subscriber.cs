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
             where T : IMQEvent
             where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, where);
        }
        public void Subscribe<T, TH>()
          where T : IMQEvent
          where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        public void Subscribe<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, subkey);
        }
        public void SubscribeBytes<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            __eventBus.SubscribeBytes<T, TH>(__name, subkey);
        }
    }
    public class DirectSubscriber : Subscriber<IDirectEventBus>
    {
        public DirectSubscriber(IDirectEventBus eventBus, string name) : base(eventBus, name)
        {
        }

        public void Subscribe<T, TH>()
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        public void SubscribeBytes<T, TH>()
            where T : IMQEvent
            where TH : IBytesHandler
        {
            __eventBus.SubscribeBytes<T, TH>(__name);
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
