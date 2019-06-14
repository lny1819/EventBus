using System;
using System.Linq.Expressions;

namespace YiDian.EventBus.Abstractions
{
    /// <summary>
    /// 广播型 EventBus
    /// </summary>
    public interface ITopicEventBus
    {
        /// <summary>
        /// 设置每个消息创建类型实例的缓存数量
        /// <para>当设置为0时表示不缓存</para>
        /// 设置缓存数量可以有效降低di容器创建类型实例的CPU消耗
        /// </summary>
        /// <param name="cacheLength"></param>
        void EnableHandlerCache(int cacheLength);
        /// <summary>
        /// 启动一个消费队列
        /// </summary>
        /// <param name="queuename">队列名称</param>
        /// <param name="action">注册订阅回调</param>
        /// <param name="fetchcount">消费队列单次从队列中最大读取消息数量</param>
        /// <param name="length">队列长度默认20000</param>
        /// <param name="autodelete">在无消费者时是否自动删除队列</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="autoAck">是否自动响应</param>
        void StartConsumer(string queuename, Action<ITopicEventBus> action, ushort fetchcount = 200, int length = 200000, bool autodelete = false, bool durable = true, bool autoAck = false);
        void DeleteQueue(string queuename, bool force);
        /// <summary>
        /// 订阅数据
        /// <para>每个协议类型可以定义约束头</para>
        /// 也可以定义约束键，发送的时候使用“约束头+顺序约束键”拼接作为key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;
        /// <summary>
        /// 订阅数据
        /// <para>每个协议类型可以定义约束头</para>
        /// 也可以定义约束键，发送的时候使用“约束头+顺序约束键”拼接作为key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="where">订阅key的约束</param>
        void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>;
        /// <summary>
        /// 订阅数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="key"></param>
        /// <param name="addHandler"></param>
        void Subscribe<T>(Expression<Func<T, bool>> where, string key, Action<KeySubHandler<T>> addHandler)
            where T : IntegrationMQEvent;
        /// <summary>
        /// 订阅数据
        /// </summary>
        /// <typeparam name="TH"></typeparam>
        /// <param name="eventName"></param>
        void Subscribe<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;
        void SetSubscribeBytesHandler(Func<string, byte[], bool> pre_handler);
        void SubscribeBytes(string key);
        void UnSubscribeBytes(string key);
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        void Unsubscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;
        void Unsubscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>;
        void Unsubscribe<TH>(string eventName)
             where TH : IDynamicIntegrationEventHandler;
        void Unsubscribe<T>(Expression<Func<T, bool>> where, string key)
            where T : IntegrationMQEvent;
        /// <summary>
        /// 发布消息
        /// <para>每个协议类型可以定义约束头</para>
        /// 也可以定义约束键，发送的时候使用“约束头+顺序约束键”拼接作为key
        /// </summary>
        /// <param name="event"></param>
        void Publish<T>(T @event) where T : IntegrationMQEvent;
        void Publish(string key, byte[] datas);
    }
}
