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
        void StartConsumer(string queuename, Action<TopicSubscriber> action, ushort fetchcount = 200, int length = 200000, bool autodelete = false, bool durable = true, bool autoAck = false);
        ///// <summary>
        ///// 订阅数据
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="where"></param>
        ///// <param name="key"></param>
        ///// <param name="addHandler"></param>
        //void Subscribe<T>(Expression<Func<T, bool>> where, string key, Action<KeySubHandler<T>> addHandler)
        //    where T : IntegrationMQEvent;
        void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>;
        void Unsubscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IntegrationMQEvent
             where TH : IIntegrationEventHandler<T>;
        void Publish<T>(T @event, string prifix) where T : IntegrationMQEvent;
        void Subscribe(string prifix, Action<KeySubHandler> action);
        void Unsubscribe(string prifix);
    }
}
