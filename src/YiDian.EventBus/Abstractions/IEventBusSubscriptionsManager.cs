using YiDian.EventBus.Abstractions;
using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBusSubscriptionsManager : IEventBusSubEventHandler
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;
        void AddDynamicSubscription<TH>(string eventName)
           where TH : IDynamicIntegrationEventHandler;

        void AddSubscription<T, TH>()
           where T : IntegrationMQEvent
           where TH : IIntegrationEventHandler<T>;
        void RemoveSubscription<T, TH>()
             where TH : IIntegrationEventHandler<T>
             where T : IntegrationMQEvent;
        void RemoveDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        bool HasSubscriptionsForEvent<T>() where T : IntegrationMQEvent;
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationMQEvent;
        string GetEventKey<T>();
    }
    public interface IEventBusSubEventHandler
    {
        Type GetEventTypeByName(string eventName);
        bool HasSubscriptionsForEvent(string eventName);
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
    }
}