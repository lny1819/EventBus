using RabbitMQ.Client;
using System;

namespace YiDian.EventBus.MQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        string Name { get; }
        event EventHandler OnConnectRecovery;
        bool IsConnected { get; }
        IEventBusSubManagerFactory SubsFactory { get; }
        bool TryConnect();

        IModel CreateModel();
    }
}
