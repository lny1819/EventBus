using System;

namespace YiDian.EventBus
{
    public interface IEventBus
    {
        void EnableHandlerCache(int cacheLength);
        void DeleteQueue(string queuename, bool force);
        void Publish<T>(T @event, bool enableTransaction = false) where T : IntegrationMQEvent;
        void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler;
        void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicBytesHandler;
        void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;
        void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent;
    }
    public interface IDirectEventBus : IEventBus
    {
        void StartConsumer(string queuename, Action<DirectSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = false, bool durable = true, bool autoAck = false);
    }
}
