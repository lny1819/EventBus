using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息订阅管理器
    /// </summary>
    public interface IEventBusSubManager
    {
        /// <summary>
        /// 
        /// </summary>
        event EventHandler<ValueTuple<string, string>> OnEventRemoved;
        event EventHandler<ValueTuple<string, string>> OnEventAdd;
        string QueueName { get; }
        void AddBytesSubscription<TH>(string subkey, string brokerName)
            where TH : IBytesHandler;
        void RemoveBytesSubscription<TH>(string subkey, string brokerName)
            where TH : IBytesHandler;
        void AddSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        void RemoveSubscription<T, TH>(string subkey, string brokerName)
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