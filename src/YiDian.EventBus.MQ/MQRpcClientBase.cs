using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
            this.methodPoll.AddOrUpdate(callmeta.MethodId, callmeta, delegate (long x, CallMeta y)
            {
                return callmeta;
            });
        }
        private void CreateConsumerChannel()
        {
            if ((this._consumerchannel == null) || this._consumerchannel.IsClosed)
            {
                if (!this._persistentConnection.IsConnected)
                {
                    this._persistentConnection.TryConnect();
                }
                Dictionary<string, object> dictionary1 = new Dictionary<string, object>();
                dictionary1.Add("x-max-length", (int)0x4e20);
                Dictionary<string, object> dictionary = dictionary1;
                IModel model = this._persistentConnection.CreateModel();
                model.QueueDeclare(this._clientName, false, true, true, (IDictionary<string, object>)dictionary);
                model.BasicQos(0, 200, false);
                model.CallbackException += (delegate (object sender, CallbackExceptionEventArgs ea)
                {
                    this._consumerchannel.Dispose();
                    this._consumerchannel = null;
                    this.CreateConsumerChannel();
                });
                this._consumerchannel = model;
                this.StartConsumer();
            }
        }
        private void StartConsumer()
        {
            EventingBasicConsumer consumer = new EventingBasicConsumer(this._consumerchannel);
            consumer.Received += (delegate (object e, BasicDeliverEventArgs o)
            {
                long num;
                CallMeta meta;
                if (long.TryParse(o.BasicProperties.CorrelationId, out num) && this.methodPoll.TryGetValue(num, out meta))
                {
                    meta.Result = o.Body;
                    meta.Reset();
                }
            });
            this._consumerchannel.BasicConsume(this._clientName, true, consumer);
        }
        public byte[] Request(string serverId, string uri, byte[] data)
        {
            string str = this.CreateServerKey(serverId).ToLower();
            CallMeta callmeta = new CallMeta();
            this.AddToMethodPool(callmeta);
            IBasicProperties basicProperties = this._consumerchannel.CreateBasicProperties();
            basicProperties.CorrelationId = ((long)callmeta.MethodId).ToString();
            basicProperties.ReplyTo = this._clientName;
            this._consumerchannel.BasicPublish("ml_rpc_event_bus", str + uri, basicProperties, data);
            callmeta.Wait(this.TimeOut);
            return this.GetMethodResult(callmeta.MethodId);
        }
        private byte[] GetMethodResult(long methodid)
        {
            if (!this.methodPoll.TryRemove(methodid, out CallMeta meta))
            {
                return null;
            }
            return (((DateTime.Now - meta.InTime).TotalSeconds <= this.TimeOut) ? meta.Result : null);
        }
        private class CallMeta : IDisposable
        {
            private static long callid;
            private readonly object async = new object();
            private AutoResetEvent autoEvent = new AutoResetEvent(false);
            public CallMeta()
            {
                MethodId = Interlocked.Increment(ref callid);
                InTime = DateTime.Now;
            }
            // Methods
            public void Dispose()
            {
                if (this.autoEvent != null)
                {
                    this.autoEvent.Dispose();
                }
                this.autoEvent = null;
            }

            public void Reset()
            {
                object async = this.async;
                lock (async)
                {
                    if (this.autoEvent != null)
                    {
                        this.autoEvent.Set();
                        this.autoEvent.Dispose();
                    }
                    this.autoEvent = null;
                }
            }

            internal void Wait(int timeOut)
            {
                object async = this.async;
                lock (async)
                {
                    if (this.autoEvent == null)
                    {
                        return;
                    }
                }
                this.autoEvent.WaitOne((int)(timeOut * 0x3e8));
            }

            // Properties
            public long MethodId { get; }

            public DateTime InTime { get; }

            public byte[] Result { get; set; }
        }
    }
}
