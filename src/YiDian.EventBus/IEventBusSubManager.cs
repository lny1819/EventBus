using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBusSubManager
    {
        event EventHandler<ValueTuple<string, string>> OnEventRemoved;
        event EventHandler<ValueTuple<string, string>> OnEventAdd;
        string QueueName { get; }
        void AddBytesSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IBytesHandler;
        void RemoveBytesSubscription<T, TH>()
            where T : IMQEvent
            where TH : IBytesHandler;
        void AddSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        void RemoveSubscription(string subkey, string brokerName);
        void RemoveSubscription<T, TH>()
             where T : IMQEvent
             where TH : IEventHandler<T>;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventKey);
        string GetEventKey<T>() where T : IMQEvent;
        string GetEventKey(Type type);
    }
    public interface IEventBusSubManagerFactory
    {
        IEventBusSubManager GetOrCreateByQueue(string queueName);
    }
}