using System;
using System.IO;
using RabbitMQ.Client;

namespace YiDian.EventBus.MQ
{
    internal class PublishPool : IDisposable
    {
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly IEventSeralize __seralize;
        readonly string BROKER_NAME;
        IModel _pubChannel;

        public PublishPool(IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, string broker)
        {
            BROKER_NAME = broker;
            __seralize = seralize;
            _persistentConnection = persistentConnection;
            CreatePublishChannel();
        }

        public void Dispose()
        {
            if (_pubChannel != null) _pubChannel.Dispose();
        }

        internal void Send<T>(T @event, string pubkey, bool enableTransaction) where T : IMQEvent
        {
            if (_pubChannel == null) return;
            using (var ms = new MemoryStream())
            {
                __seralize.Serialize(ms, @event);
                _pubChannel.BasicPublish(exchange: BROKER_NAME,
                                 routingKey: pubkey,
                                 basicProperties: null,
                                 body: ms.GetBuffer());
            }
        }
        void CreatePublishChannel()
        {
            if (_pubChannel == null || _pubChannel.IsClosed)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                //_pubChannel.ConfirmSelect();
                _pubChannel = _persistentConnection.CreateModel();
                _pubChannel.CallbackException += (sender, ea) =>
                {
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }
    }
}
