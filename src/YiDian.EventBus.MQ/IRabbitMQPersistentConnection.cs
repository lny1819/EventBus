using RabbitMQ.Client;
using System;

namespace YiDian.EventBus.MQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        event EventHandler<string> ConnectFail;
        string Name { get; }
        bool IsConnected { get; }
        IEventBusSubManagerFactory SubsFactory { get; }
        bool TryConnect();

        IModel CreateModel();
    }
}
