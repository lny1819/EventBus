using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        event EventHandler<string> OnEventRemoved;
        event EventHandler<string> OnEventAdd;
        string QueueName { get; }
        void AddSubscription<TH>(string eventName) where TH : IDynamicBytesHandler;
        void RemoveSubscription<TH>(string eventName) where TH : IDynamicBytesHandler;
        void AddSubscription<T, TH>()
           where T : IntegrationMQEvent
           where TH : IIntegrationEventHandler<T>;
        void AddSubscription<T, TH>(string eventName)
           where T : IntegrationMQEvent
           where TH : IIntegrationEventHandler<T>;
        void RemoveSubscription<T, TH>()
             where TH : IIntegrationEventHandler<T>
             where T : IntegrationMQEvent;
        void RemoveSubscription<T, TH>(string eventName)
             where TH : IIntegrationEventHandler<T>
             where T : IntegrationMQEvent;

        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        string GetEventKey(string eventName);
        string GetEventKey<T>() where T : IntegrationMQEvent;
    }
    public interface IEventBusSubscriptionsManagerFactory
    {
        IEventBusSubscriptionsManager GetOrCreateByQueue(string queueName);
    }
}