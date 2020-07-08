using System;
using System.IO;
using System.Threading;
using RabbitMQ.Client;

namespace YiDian.EventBus.MQ
{
    internal class PublishPool : IDisposable
    {
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly IEventSeralize __seralize;
        readonly string BROKER_NAME;
        //IModel _pubChannel1;
        IModel _pubChannel2;
        //int _index;


        public PublishPool(IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, string broker)
        {
            BROKER_NAME = broker;
            __seralize = seralize;
            _persistentConnection = persistentConnection;
            //CreatePublishChannel1();
            CreatePublishChannel2();
        }

        public void Dispose()
        {
            //if (_pubChannel1 != null) _pubChannel1.Dispose();
            if (_pubChannel2 != null) _pubChannel2.Dispose();
        }

        internal void Send<T>(T @event, string pubkey, bool enableTransaction, out int length) where T : IMQEvent
        {
            //IModel channel = null;
            //var index = Interlocked.Increment(ref _index) % 2;
            //if (index == 1) channel = _pubChannel1;
            //if (index == 0) channel = _pubChannel2;
            //if (channel == null) return;
            var data = __seralize.Serialize(@event);
            length = data.Length;
            //var batch = _pubChannel2.CreateBasicPublishBatch();
            //batch.Add(BROKER_NAME, pubkey, false, null, data.ToArray());
            //batch.Publish();
            _pubChannel2.BasicPublish(exchange: BROKER_NAME,
                             routingKey: pubkey,
                             basicProperties: null,
                             body: data.ToArray());
        }
        //void CreatePublishChannel1()
        //{
        //    if (_pubChannel1 == null || _pubChannel1.IsClosed)
        //    {
        //        if (!_persistentConnection.IsConnected)
        //        {
        //            _persistentConnection.TryConnect();
        //        }
        //        //_pubChannel.ConfirmSelect();
        //        _pubChannel1 = _persistentConnection.CreateModel();
        //        _pubChannel1.CallbackException += (sender, ea) =>
        //        {
        //            _pubChannel1.Dispose();
        //            _pubChannel1 = null;
        //            CreatePublishChannel1();
        //        };
        //    }
        //}
        void CreatePublishChannel2()
        {
            if (_pubChannel2 == null || _pubChannel2.IsClosed)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                //_pubChannel.ConfirmSelect();
                _pubChannel2 = _persistentConnection.CreateModel();
                _pubChannel2.CallbackException += (sender, ea) =>
                {
                    _pubChannel2.Dispose();
                    _pubChannel2 = null;
                    CreatePublishChannel2();
                };
            }
        }
    }
}
