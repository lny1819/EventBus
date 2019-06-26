using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBusSubManager
    {
        event EventHandler<string> OnEventRemoved;
        event EventHandler<string> OnEventAdd;
        string QueueName { get; }
        void AddBytesSubscription<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IBytesHandler;
        void RemoveBytesSubscription<T, TH>()
            where T : IMQEvent
            where TH : IBytesHandler;
        void AddSubscription<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        void RemoveSubscription(string subkey);
        void RemoveSubscription<T, TH>()
             where T : IMQEvent
             where TH : IEventHandler<T>;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventKey);
        string GetEventKey<T>() where T : IMQEvent;
    }
    public interface IEventBusSubManagerFactory
    {
        IEventBusSubManager GetOrCreateByQueue(string queueName);
    }
}