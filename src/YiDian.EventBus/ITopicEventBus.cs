using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    /// <summary>
    /// Topic型 EventBus
    /// </summary>
    public interface ITopicEventBus : IEventBus
    {
        /// <summary>
        /// 通过自定义路由键前缀来发送消息 当消息体有keyIndex标识时 会将标识拼接在路由键的头部
        /// <para>总是在路由键后追加消息在<see cref="IAppEventsManager"/>中的定义</para>
        /// 如key=x, 在<see cref="IAppEventsManager"/>中的定义为y 则最终的路由键为x.y
        ///  <para>当此消息总线是<see cref="ITopicEventBus"/>总线类型且消息体有keyIndex标识时 路由键为：a.b.c.x.y（a,b,c未keyIndex标识定义） ；没有keyIndex标识时，为x.y</para>
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="event">消息</param>
        /// <param name="key">路由键前缀</param>
        /// <param name="tag">当启用发送确认时，返回发送消息的tag</param>
        /// <param name="enableTransaction">启用发送确认</param>
        bool Publish<T>(T @event, string key, out ulong tag, bool enableTransaction = false) where T : IMQEvent;
        /// <summary>
        /// 注册消息队列机器队列消费消息方法
        /// </summary>
        /// <param name="queuename">消费队列名称</param>
        /// <param name="action">订阅队列消费消息类型</param>
        /// <param name="fetchCount">每次最多获取消息数量</param>
        /// <param name="queueLength">队列最大数据存储长度</param>
        /// <param name="autodel">是否自动删除队列</param>
        /// <param name="durable">队列数据是否持久化</param>
        /// <param name="autoAck">队列数据是否自动确认</param>
        /// <param name="autoStart">是否在注册完成后开启消息消费</param>
        void RegisterConsumer(string queuename, Action<TopicSubscriber> action, ushort fetchCount = 200, int queueLength = 200000, bool autodel = false, bool durable = true, bool autoAck = false, bool autoStart = true);
        /// <summary>
        /// 通过指定表达式订阅满足表达式条件的消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="where">消息条件</param>
        void Subscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="where">消息条件</param>
        void Unsubscribe<T, TH>(string queueName, Expression<Func<T, bool>> where)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        /// <summary>
        /// 通过指定的路由键前缀订阅消息，可使用*和#
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="subkey">路由键前缀</param>
        void Subscribe<T, TH>(string queueName, string subkey)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="subkey">路由键前缀</param>
        void Unsubscribe<T, TH>(string queueName, string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>;
        /// <summary>
        /// 在指定队列上消费消息
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        void SubscribeBytes<TH>(string queueName)
          where TH : IBytesHandler;
        /// <summary>
        /// 移除指定队列上消费消息
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        void UnsubscribeBytes<TH>(string queueName)
            where TH : IBytesHandler;
        /// <summary>
        /// 通过指定的路由键前缀订阅动态消息，可使用*和#
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="subkey">路由键前缀</param>
        void SubscribeBytes<TH>(string queueName, string subkey)
            where TH : IBytesHandler;
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="subkey">路由键前缀</param>
        void UnsubscribeBytes<T, TH>(string queueName, string subkey)
            where TH : IBytesHandler;
    }
}
