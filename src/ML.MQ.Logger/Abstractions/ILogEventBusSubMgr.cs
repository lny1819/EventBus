using ML.EventBus;
using ML.MqLogger.Logmodel;
using ML.MqLogger.MQLogsEventBus;
using System;
using System.Collections.Generic;

namespace ML.MqLogger.Abstractions
{
    public interface ILogEventBusSubMgr
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;
        IEnumerable<SubInfo> GetHandlersForEvent(string eventName,out Type eventType);
        bool HasSubscriptionsForEvent(string name);
        void AddSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : ILogEventHandler<T>;
        void RemoveSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : ILogEventHandler<T>;
    }
}
