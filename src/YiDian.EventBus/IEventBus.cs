using System;

namespace YiDian.EventBus
{
    public interface IEventBus : IDisposable
    {
        string BROKER_NAME { get; }
        string ConnectionName { get; }
        event EventHandler<Exception> OnUncatchException;
        void EnableHandlerCache(int cacheLength);
        void DeleteQueue(string queuename, bool force);
        void Start(string queueName);
        /// <summary>
        /// 发布消息   当消息体有keyIndex标识时 会将标识拼接在路由键的头部
        /// <para>路由键的最后一个标识时消息体在<see cref="IAppEventsManager">中的名称</para>
        /// 路由键格式：当消息体有keyIndex标识时 a.b.c.x ;没有keyIndex标识时，为x
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="enableTransaction"></param>
        int Publish<T>(T @event, bool enableTransaction = false) where T : IMQEvent;
        /// <summary>
        /// 自定义路由键发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="key"></param>
        /// <param name="enableTransaction"></param>
        void PublishWithKey<T>(T @event, string key, bool enableTransaction = false) where T : IMQEvent;

        void Subscribe<T, TH>(string queueName)
             where T : IMQEvent
             where TH : IEventHandler<T>;
        void Unsubscribe<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IEventHandler<T>;
    }
    public interface IDirectEventBus : IEventBus
    {
        void SubscribeBytes<T, TH>(string queueName)
          where T : IMQEvent
          where TH : IBytesHandler;
        void UnsubscribeBytes<T, TH>(string queueName)
            where T : IMQEvent
            where TH : IBytesHandler;
        void RegisterConsumer(string queuename, Action<DirectSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = false, bool durable = true, bool autoAck = false, bool autoStart = true);
    }
}
