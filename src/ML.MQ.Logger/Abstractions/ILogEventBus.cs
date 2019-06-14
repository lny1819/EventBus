using ML.EventBus;
using ML.MqLogger.Abstractions;
using ML.MqLogger.Logmodel;
using System;

namespace ML.MqLogger.MQLogsEventBus.Abstractions
{
    public interface ILogEventBus
    {
        void StartConsumer(string clientName, Action<ILogEventBus> action, ushort fetchCount = 200, int queueLength = 2000000, bool autodel = false, bool durable = true);
        void Publish(LoggerEvent @event);
        void Subscribe<T, TH>() where T : IntegrationMQEvent where TH : ILogEventHandler<T>;

        void Unsubscribe<T, TH>() where T : IntegrationMQEvent where TH : ILogEventHandler<T>;
    }
}
