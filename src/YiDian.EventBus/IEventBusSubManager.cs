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
        /// 最后一个消费方法被移除时触发
        /// </summary>
        event EventHandler<ValueTuple<string, string>> OnEventRemoved;
        /// <summary>
        /// 第一个消费方法被添加时触发
        /// </summary>
        event EventHandler<ValueTuple<string, string>> OnEventAdd;
        /// <summary>
        /// 消费队列名称
        /// </summary>
        string QueueName { get; }
        /// <summary>
        /// 订阅动态消息
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="subkey">订阅KEY</param>
        /// <param name="brokerName">订阅交换机名称</param>
        void AddBytesSubscription<TH>(string subkey, string brokerName)
            where TH : IBytesHandler;
        /// <summary>
        /// 移除订阅的动态消息
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="subkey">订阅KEY</param>
        /// <param name="brokerName">订阅交换机名称</param>
        void RemoveBytesSubscription<TH>(string subkey, string brokerName)
            where TH : IBytesHandler;
        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="subkey">订阅KEY</param>
        /// <param name="brokerName">订阅交换机名称</param>
        void AddSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        /// <summary>
        /// 移除订阅的消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="subkey">订阅KEY</param>
        /// <param name="brokerName">订阅交换机名称</param>
        void RemoveSubscription<T, TH>(string subkey, string brokerName)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        /// <summary>
        /// 获取指定消息key的所有消费类型
        /// </summary>
        /// <param name="eventKey">订阅KEY</param>
        /// <param name="brokerName">交换机名称</param>
        /// <returns></returns>
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventKey, string brokerName);
        /// <summary>
        /// 获取消息key
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <returns></returns>
        string GetEventKey<T>() where T : IMQEvent;
        /// <summary>
        /// 获取消息key
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <returns></returns>
        string GetEventKey(Type type);
        /// <summary>
        /// 获取指定消息key的所有动态消费类型
        /// </summary>
        /// <param name="key">订阅KEY</param>
        /// <param name="brokerName">交换机名称</param>
        /// <param name="match">是否模糊匹配Key</param>
        /// <returns></returns>
        IEnumerable<SubscriptionInfo> GetDymaicHandlersBySubKey(string key, string brokerName, bool match = false);
    }
    /// <summary>
    /// 订阅管理器工厂接口
    /// </summary>
    public interface IEventBusSubManagerFactory
    {
        /// <summary>
        /// 通过指定的消费队列名称创建一个消息订阅管理器实例
        /// </summary>
        /// <param name="queueName">消费队列名称</param>
        /// <returns></returns>
        IEventBusSubManager GetOrCreateByQueue(string queueName);
    }
}