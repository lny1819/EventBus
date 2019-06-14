using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        event EventHandler<string> OnEventRemoved;
        bool IsEmpty { get; }
        void AddDynamicSubscription<TH>(string eventName) where TH : IDynamicBytesHandler;
        void RemoveDynamicSubscription<TH>(string eventName) where TH : IDynamicBytesHandler;
        string GetEventKey<T>()
           where T : IntegrationMQEvent;
        string GetEventKey(string key);
        void AddSubscription<T, TH>()
           where T : IntegrationMQEvent
           where TH : IIntegrationEventHandler<T>;
        void RemoveSubscription<T, TH>()
             where TH : IIntegrationEventHandler<T>
             where T : IntegrationMQEvent;
        bool HasSubscriptionsForEvent(string eventName);
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
    }
}