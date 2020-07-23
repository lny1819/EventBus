using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace YiDian.EventBus.MQ
{
    internal class PublishPool : IDisposable
    {
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly IEventSeralize __seralize;
        readonly string BROKER_NAME;
        IModel _pubC;
        ILogger _logger;
        readonly bool _allwaysEnableTrans;
        public event EventHandler<ConfirmArg> OnConfirm;

        public PublishPool(ILogger logger, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, string broker, bool allwaysEnableTrans)
        {
            BROKER_NAME = broker;
            __seralize = seralize;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "not set logger");
            _persistentConnection = persistentConnection;
            _allwaysEnableTrans = allwaysEnableTrans;
            CreatePublishChannel();
        }
        public void Dispose()
        {
            if (_pubC != null) _pubC.Dispose();
        }

        internal bool Send<T>(T @event, string pubkey, bool enableTransaction, out int length, out ulong seq_no, int trans_time_out = 10) where T : IMQEvent
        {
            seq_no = 0;
            var data = __seralize.Serialize(@event);
            length = data.Length;
            if (!_allwaysEnableTrans && enableTransaction) _pubC.ConfirmSelect();
            if (_allwaysEnableTrans || enableTransaction) seq_no = _pubC.NextPublishSeqNo;
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning("" + ex.Message);
                }
            );
            policy.Execute(() =>
            {
                _pubC.BasicPublish(exchange: BROKER_NAME,
                             routingKey: pubkey,
                             basicProperties: null,
                             body: data.ToArray());
            });
            if (!_allwaysEnableTrans && enableTransaction)
            {
                if (!_pubC.WaitForConfirms(new TimeSpan(0, 0, trans_time_out)))
                {
                    _logger.LogError("message sending timeout,send key is:" + pubkey);
                    return false;
                }
            }
            return true;
        }
        void CreatePublishChannel()
        {
            if (_pubC == null || _pubC.IsClosed)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                _pubC = _persistentConnection.CreateModel();
                if (_allwaysEnableTrans)
                {
                    _pubC.ConfirmSelect();
                    _pubC.BasicAcks += PubC_BasicAcks;
                    _pubC.BasicNacks += PubC_BasicNacks;
                }
                _pubC.CallbackException += (sender, ea) =>
                {
                    _pubC.Dispose();
                    _pubC = null;
                    CreatePublishChannel();
                };
            }
        }

        private void PubC_BasicNacks(object sender, RabbitMQ.Client.Events.BasicNackEventArgs e)
        {
            OnConfirm(sender, new ConfirmArg() { IsOk = false, Multiple = e.Multiple, Tag = e.DeliveryTag });
        }

        private void PubC_BasicAcks(object sender, RabbitMQ.Client.Events.BasicAckEventArgs e)
        {
            OnConfirm(sender, new ConfirmArg() { IsOk = true, Multiple = e.Multiple, Tag = e.DeliveryTag });
        }
    }
}
