using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using YiDian.Soa.Sp;

namespace YiDian.EventBus.MQ
{
    internal class MQRpcClientBase
    {
        public const string BROKER_NAME = "rpc_event_bus";
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly string _clientName;
        readonly IQpsCounter _qps;
        readonly AutoResetEvent signal;
        ILogger _logger;
        private readonly ConcurrentDictionary<long, CallMeta> methodPoll;
        IModel _consumerchannel;
        public MQRpcClientBase(IRabbitMQPersistentConnection rabbitMQPersistentConnection, string clientName, ILogger logger, IQpsCounter counter)
        {
            signal = new AutoResetEvent(false);
            _clientName = clientName ?? throw new ArgumentNullException(nameof(clientName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _qps = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            _persistentConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            methodPoll = new ConcurrentDictionary<long, CallMeta>();
            CreateConsumerChannel();
        }
        public bool IsConnnected { get { return _persistentConnection.IsConnected; } }
        private string CreateServerKey(string serverName)
        {
            if (serverName.IndexOf(".") > -1)
            {
                serverName.Replace('.', '_');
            }
            return serverName;
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
                model.QueueDeclare(_clientName, false, true, true, dictionary);
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

        internal Task<ReadOnlyMemory<byte>> Request(string serverId, ReadOnlyMemory<byte> readOnlyMemory, out long mid)
        {
            string str = CreateServerKey(serverId).ToLower();
            var callmeta = new CallMeta();
            mid = callmeta.MethodId;
            AddToMethodPool(callmeta);
            var basicProperties = _consumerchannel.CreateBasicProperties();
            basicProperties.CorrelationId = callmeta.MethodId.ToString();
            basicProperties.ReplyTo = _clientName;
            var policy = Policy.Handle<SocketException>()
              .Or<BrokerUnreachableException>()
              .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
              {
                  _logger.LogWarning("" + ex.Message);
              }
          );
            policy.Execute(() =>
            {
                _consumerchannel.BasicPublish(BROKER_NAME, str, basicProperties, readOnlyMemory.ToArray());
            });
            return callmeta.Task;
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
        class CallMeta : TaskCompletionSource<ReadOnlyMemory<byte>>
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
