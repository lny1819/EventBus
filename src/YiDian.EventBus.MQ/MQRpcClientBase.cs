﻿using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YiDian.Soa.Sp;

namespace YiDian.EventBus.MQ
{
    public class MQRpcClientBase
    {
        public const string BROKER_NAME = "rpc_event_bus";
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly string _clientName;
        readonly IQpsCounter _qps;
        readonly AutoResetEvent signal;
        private readonly ConcurrentDictionary<long, CallMeta> methodPoll;
        IModel _consumerchannel;
        public MQRpcClientBase(IRabbitMQPersistentConnection rabbitMQPersistentConnection, string clientName, ILogger logger, IQpsCounter counter, int timeOut = 10)
        {
            signal = new AutoResetEvent(false);
            TimeOut = timeOut;
            _clientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
            _qps = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            _persistentConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            methodPoll = new ConcurrentDictionary<long, CallMeta>();
            CreateConsumerChannel();
        }
        public int TimeOut { get; }
        private string CreateServerKey(string serverName)
        {
            if (serverName.IndexOf(".") > -1)
            {
                serverName.Replace('.', '_');
            }
            return (serverName + ".");
        }
        private void AddToMethodPool(CallMeta callmeta)
        {
            methodPoll.AddOrUpdate(callmeta.MethodId, callmeta, (x, y) => y);
        }
        private void CreateConsumerChannel()
        {
            if ((_consumerchannel == null) || _consumerchannel.IsClosed)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                Dictionary<string, object> dictionary1 = new Dictionary<string, object>
                {
                    { "x-max-length", 20000 }
                };
                Dictionary<string, object> dictionary = dictionary1;
                IModel model = _persistentConnection.CreateModel();
                model.QueueDeclare(_clientName, false, true, true, (IDictionary<string, object>)dictionary);
                model.BasicQos(0, 200, false);
                model.CallbackException += (delegate (object sender, CallbackExceptionEventArgs ea)
                {
                    _consumerchannel.Dispose();
                    _consumerchannel = null;
                    CreateConsumerChannel();
                });
                _consumerchannel = model;
                StartConsumer();
            }
        }
        private void StartConsumer()
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(_consumerchannel);
            consumer.Received += Consumer_Received;
            _consumerchannel.BasicConsume(_clientName, true, consumer);
        }

        private void Consumer_Received(object e, BasicDeliverEventArgs o)
        {
            if (long.TryParse(o.BasicProperties.CorrelationId, out long num) && methodPoll.TryRemove(num, out CallMeta meta))
            {
                meta.SetResult(o.Body);
            }
        }

        public Task<byte[]> Request(string serverId, string uri, byte[] data)
        {
            string str = CreateServerKey(serverId).ToLower();
            var callmeta = new CallMeta();
            AddToMethodPool(callmeta);
            var basicProperties = _consumerchannel.CreateBasicProperties();
            basicProperties.CorrelationId = callmeta.MethodId.ToString();
            basicProperties.ReplyTo = _clientName;
            _consumerchannel.BasicPublish(BROKER_NAME, str + uri, basicProperties, data);
            return callmeta.Task;
        }
        class CallMeta : TaskCompletionSource<byte[]>
        {
            private static long callid;
            public long MethodId { get; }
            public DateTime InTime { get; private set; }

            public CallMeta()
            {
                MethodId = Interlocked.Increment(ref callid);
                InTime = DateTime.Now;
            }
        }
    }
}
