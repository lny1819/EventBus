using System;

namespace YiDian.EventBus
{
    public interface IEventBus
    {
        event EventHandler<Exception> OnUncatchException;
        void EnableHandlerCache(int cacheLength);
        void DeleteQueue(string queuename, bool force);
        void Start(string queueName);
        void Publish<T>(T @event, bool enableTransaction = false) where T : IMQEvent;
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
