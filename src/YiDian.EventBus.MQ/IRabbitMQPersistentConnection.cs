using RabbitMQ.Client;
using System;

namespace YiDian.EventBus.MQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        event EventHandler OnConnectRecovery;
        bool IsConnected { get; }
        IAppEventsManager EventsManager { get; }
        IEventSeralize Seralizer { get; }
        bool TryConnect();

        IModel CreateModel();
    }
}
