using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    /// <summary>
    /// 广播型 EventBus
    /// </summary>
    public interface ITopicEventBus : IEventBus
    {
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
        void RegisterConsumer(string queuename, Action<TopicSubscriber> action, ushort fetchcount = 200, int length = 200000, bool autodelete = false, bool durable = true, bool autoAck = false, bool autoStart = true);
        bool PublishPrefix<T>(T @event, string fix, out ulong tag, bool enableTransaction = false) where T : IMQEvent;
        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <typeparam name="T">消息体</typeparam>
        /// <typeparam name="TH">消息回调</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="where">消息条件</param>
        void Subscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        void Unsubscribe<T>(string queueName, Expression<Func<T, bool>> where)
             where T : IMQEvent;
        void Subscribe<T, TH>(string queueName, string subkey)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        void Unsubscribe(string queueName, string subkey);
        void SubscribeBytes<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IBytesHandler;
    }
}
