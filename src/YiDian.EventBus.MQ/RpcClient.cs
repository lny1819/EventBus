using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace YiDian.EventBus.MQ
{
    public class RpcClient : IRpcClient
    {
        public const string BROKER_NAME = "ml_rpc_event_bus";
        readonly IRabbitMQPersistentConnection _persistentConnection;
        readonly ConcurrentDictionary<long, CallMeta> methodPoll;
        readonly string _clientName;
        readonly IQpsCounter _qps;

        IModel _consumerchannel;
        public RpcClient(IRabbitMQPersistentConnection rabbitMQPersistentConnection, string clientName, ILogger logger, IQpsCounter counter, int timeOut = 10)
        {
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
            return serverName + ".";
        }

        private void CreateConsumerChannel()
        {
            if (_consumerchannel != null && !_consumerchannel.IsClosed) return;
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var dic = new Dictionary<string, object>
            {
                //消费队列最大消息数量
                ["x-max-length"] = 20000
            };
            var channel = _persistentConnection.CreateModel();
            channel.QueueDeclare(_clientName, false, true, true, dic);
            channel.BasicQos(0, 200, false);
            channel.CallbackException += (sender, ea) =>
            {
                _consumerchannel.Dispose();
                _consumerchannel = null;
                CreateConsumerChannel();
            };
            _consumerchannel = channel;
            StartConsumer();
        }
        private void StartConsumer()
        {
            var consumer = new EventingBasicConsumer(_consumerchannel);
            consumer.Received += (e, o) =>
            {
                if (long.TryParse(o.BasicProperties.CorrelationId, out long id))
                {
                    var flag = methodPoll.TryGetValue(id, out CallMeta meta);
                    if (flag)
                    {
                        meta.Result = o.Body;
                        meta.Reset();
                    }
                }
            };
            _consumerchannel.BasicConsume(_clientName, true, consumer);
        }
        public byte[] Request(string serverId, string uri, byte[] data)
        {
            var id = CreateServerKey(serverId).ToLower();
            var callmeta = new CallMeta();
            AddToMethodPool(callmeta);
            var props = _consumerchannel.CreateBasicProperties();
            props.CorrelationId = callmeta.MethodId.ToString();
            props.ReplyTo = _clientName;
            _consumerchannel.BasicPublish(BROKER_NAME, routingKey: id + uri, basicProperties: props, body: data);
            callmeta.Wait(TimeOut);
            return GetMethodResult(callmeta.MethodId);
        }

        private void AddToMethodPool(CallMeta callmeta)
        {
            methodPoll.AddOrUpdate(callmeta.MethodId, callmeta, (x, y) => callmeta);
        }

        private byte[] GetMethodResult(long methodid)
        {
            var flag = methodPoll.TryRemove(methodid, out CallMeta meta);
            if (!flag) return null;
            var span = DateTime.Now - meta.InTime;
            if (span.TotalSeconds > TimeOut) return null;
            return meta.Result;
        }

        private class CallMeta : IDisposable
        {
            static long callid = 0;
            object async = new object();
            private AutoResetEvent autoEvent;
            public CallMeta()
            {
                autoEvent = new AutoResetEvent(false);
                MethodId = Interlocked.Increment(ref callid);
                InTime = DateTime.Now;
            }
            public long MethodId { get; }
            public DateTime InTime { get; }
            public byte[] Result { get; set; }
            public void Dispose()
            {
                if (autoEvent != null) autoEvent.Dispose();
                autoEvent = null;
            }

            public void Reset()
            {
                lock (async)
                {
                    if (autoEvent != null)
                    {
                        autoEvent.Set();
                        autoEvent.Dispose();
                    }
                    autoEvent = null;
                }
            }

            internal void Wait(int timeOut)
            {
                lock (async)
                {
                    if (autoEvent == null) return;
                }
                autoEvent.WaitOne(timeOut * 1000);
            }
        }
    }
}
