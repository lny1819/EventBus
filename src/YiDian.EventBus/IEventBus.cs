using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IEventBus
    {
        event EventHandler<Exception> OnUncatchException;
        void EnableHandlerCache(int cacheLength);
        void DeleteQueue(string queuename, bool force);
        void PublishBytes(byte[] data, string eventName, bool enableTransaction = false);
        void Publish<T>(T @event, bool enableTransaction = false) where T : IntegrationMQEvent;
        void Subscribe<TH>(string queueName, string eventName)
            where TH : IDynamicBytesHandler;
        void Unsubscribe<TH>(string queueName, string eventNamee)
            where TH : IDynamicBytesHandler;
        void Subscribe<T, TH>(string queueName)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;
        void Unsubscribe<T, TH>(string queueName)
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent;
        List<Type> GetAppEvents(string appname);
    }
    public interface IDirectEventBus : IEventBus
    {
        void StartConsumer(string queuename, Action<DirectSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = false, bool durable = true, bool autoAck = false);
    }
}
