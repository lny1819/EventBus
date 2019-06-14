using RabbitMQ.Client;
using System;

namespace YiDian.EventBusMQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        event EventHandler OnConnectRecovery;
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
