using System;

namespace YiDian.EventBus
{
    /// <summary>
    /// 定义消息总线的基本方法
    /// </summary>
    public interface IEventBus : IDisposable
    {
        /// <summary>
        /// 交换机名称
        /// </summary>
        string BROKER_NAME { get; }
        /// <summary>
        /// MQ连接命名称，既在连接字符串中的name定义；当name为空或未配置时，返回default
        /// </summary>
        string ConnectionName { get; }
        /// <summary>
        /// 执行消费消息的方法时出错
        /// </summary>
        event EventHandler<Exception> OnUncatchException;
        /// <summary>
        /// 缓存消费类型实例
        /// </summary>
        /// <param name="cacheLength">缓存实例的数量</param>
        void EnableHandlerCache(int cacheLength);
        /// <summary>
        /// 删除队列
        /// </summary>
        /// <param name="queuename"></param>
        /// <param name="force"></param>
        void DeleteQueue(string queuename, bool force);
        /// <summary>
        /// 开始指定队列名称的消息消费
        /// </summary>
        /// <param name="queueName"></param>
        void Start(string queueName);
        /// <summary>
        /// 总是启用消息发送确认模式
        /// </summary>
        /// <param name="action">消息确认回调方法</param>
        void EnablePubTrans(EventHandler<ConfirmArg> action);
        /// <summary>
        /// 发布消息
        ///  <para>总是在路由键后追加消息在<see cref="IAppEventsManager"/>中的定义</para>
        ///  如 在<see cref="IAppEventsManager"/>中的定义为y 则最终的路由键为y
        /// <para>当此消息总线是<see cref="ITopicEventBus"/>总线类型且消息体有<see cref="KeyIndex"/>标识时 路由键为：a.b.c.y（a,b,c未keyIndex标识定义） ；没有keyIndex标识时，为y</para>
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="event">消息</param>
        /// <param name="tag">当启用发送确认时，返回发送消息的tag</param>
        /// <param name="enableTransaction">启用发送确认</param>
        bool Publish<T>(T @event, out ulong tag, bool enableTransaction = false) where T : IMQEvent;
        /// <summary>
        /// 发送消息
        /// <para>总是在路由键后追加消息在<see cref="IAppEventsManager"/>中的定义</para>
        /// 如 在<see cref="IAppEventsManager"/>中的定义为y 则最终的路由键为y
        /// <para>当此消息总线是<see cref="ITopicEventBus"/>总线类型且消息体有<see cref="KeyIndex"/>标识时 路由键为：a.b.c.y（a,b,c未keyIndex标识定义） ；没有keyIndex标识时，为y</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <returns></returns>
        bool Publish<T>(T @event) where T : IMQEvent;
        /// <summary>
        /// 发送字节流消息
        /// 此类型消息只能通过<see cref="IBytesHandler"/>来接收处理
        /// </summary>
        /// <param name="datas">字节流消息</param>
        /// <param name="key">路由键</param>
        /// <param name="tag">当启用发送确认时，返回发送消息的tag</param>
        /// <param name="enableTransaction">启用发送确认</param>
        /// <returns></returns>
        bool Publish(ReadOnlyMemory<byte> datas, string key, out ulong tag, bool enableTransaction = false);
        /// <summary>
        /// 在指定队列上消费消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        void Subscribe<T, TH>(string queueName)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        /// <summary>
        /// 移除指定队列上消费消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="queueName">队列名称</param>
        void Unsubscribe<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
    }

    public class ConfirmArg : EventArgs
    {
        public bool IsOk { get; set; }
        public ulong Tag { get; set; }
        public bool Multiple { get; set; }
    }
}
