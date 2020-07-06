using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using Autofac;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using YiDian.Soa.Sp;
using YiDian.EventBus.MQ.Rpc.Route;
using YiDian.EventBus.MQ.Rpc.Abstractions;

namespace YiDian.EventBus.MQ.Rpc
{
    public class RPCServer : IRpcServer
    {
        const string BROKER_NAME = "rpc_event_bus";
        const string AUTOFAC_NAME = "rpc_event_bus";
        readonly ILogger _logger;
        readonly ILifetimeScope _autofac;
        readonly IRabbitMQPersistentConnection _conn;
        readonly IQpsCounter _qps;
        readonly TaskFactory _factory;
        IModel _consumerChannel;
        IModel _pubChannel;
        internal RPCServer(IRabbitMQPersistentConnection conn, ILogger logger, RpcServerConfig config, ILifetimeScope autofac, IQpsCounter qps, IEventSeralize seralize = null)
        {
            config.ApplicationId = config.ApplicationId.ToLower();
            RoutingTables.LoadControlers(config.ApplicationId);
            Seralize = seralize ?? new DefaultSeralizer();
            _autofac = autofac;
            _logger = logger;
            _conn = conn;
            _qps = qps;
            Configs = config;

            RoutePrefix = string.Format($"{config.ApplicationId}.#");
            CreatePublishChannel();
            CreateConsumerChannel();
        }
        void CreatePublishChannel()
        {
            if (_pubChannel == null || _pubChannel.IsClosed)
            {
                if (!_conn.IsConnected)
                {
                    _conn.TryConnect();
                }
                _pubChannel = _conn.CreateModel();
                _pubChannel.CallbackException += (sender, ea) =>
                {
                    _pubChannel.Dispose();
                    _pubChannel = null;
                    CreatePublishChannel();
                };
            }
        }
        internal static string RoutePrefix { get; private set; }
        public RpcServerConfig Configs { get; }

        public IEventSeralize Seralize { get; }

        public string ServerId => Configs.ApplicationId;

        private void CreateConsumerChannel()
        {
            if (_consumerChannel != null && !_consumerChannel.IsClosed) return;
            if (!_conn.IsConnected) _conn.TryConnect();
            var channel = _conn.CreateModel();
            channel.ExchangeDeclare(BROKER_NAME, "topic", true, false);
            channel.BasicQos(0, 1, false);
            channel.QueueDeclare(queue: Configs.ApplicationId, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(Configs.ApplicationId, BROKER_NAME, RoutePrefix, null);
            _consumerChannel = channel;
            StartConsumer();
        }
        private void StartConsumer()
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, ea) =>
            {
                _qps.Add("consumer");
                ProcessEvent(ea);
            };
            _consumerChannel.BasicConsume(queue: Configs.ApplicationId, autoAck: true, consumer: consumer);
        }


        private void ProcessEvent(BasicDeliverEventArgs ea)
        {
            var action = RoutingTables.Route(ea.RoutingKey, Configs.ApplicationId, out string msg);
            if (action == null)
            {
                ReplayTo(ea, 401, msg);
                return;
            }
            var header = new HeadersAnalysis(ea.Body);
            var clienttime = UnixTimestampToDate(BitConverter.ToInt64(ea.Body, 0));
            var span = Math.Abs((DateTime.Now - clienttime).TotalSeconds);
            if (span > 10)
            {
                ReplayTo(ea, 402, $"请求已超时 请求 {ea.RoutingKey} 耗时 {span.ToString()}ms");
                return;
            }
            Excute(action, ea);
            //_factory.StartNew(() => Excute(action, ea, stopwatch)).ContinueWith(x =>
            //{
            //    if (x.Status == TaskStatus.Faulted)
            //    {
            //        _logger?.LogError(x.Exception.ToString());
            //        stopwatch.Stop();
            //        _logger.LogError($" 请求出错 {ea.RoutingKey} 耗时 {stopwatch.ElapsedMilliseconds.ToString()}ms");
            //    }
            //});
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReplayTo(BasicDeliverEventArgs ea, int state, string msg, object obj = null, Type objType = null)
        {
            var replyTo = ea.BasicProperties.ReplyTo;
            var replyTold = ea.BasicProperties.CorrelationId;
            var replyProps = _pubChannel.CreateBasicProperties();
            replyProps.CorrelationId = replyTold;
            var bs = Seralize.Serialize(obj, objType);
            _pubChannel.BasicPublish("", routingKey: replyTo, basicProperties: replyProps, body: bs);
        }

        readonly static DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime UnixTimestampToDate(long timestamp)
        {
            return start.AddSeconds(timestamp).ToLocalTime();
        }
        class Token
        {
            public RouteAction Action { get; set; }
            public BasicDeliverEventArgs Eargs { get; set; }
            public object Data { get; set; }
            public DateTime InTime { get; set; }
        }
        private void Excute(RouteAction route_action, BasicDeliverEventArgs ea)
        {
            object invoke_data = null;
            if (route_action.InArgumentType != null) invoke_data = Seralize.DeserializeObject(ea.Body, route_action.InArgumentType);
            var token = new Token() { InTime = DateTime.Now, Action = route_action, Data = invoke_data, Eargs = ea };
            var action = token.Action;
            var argu = token.Data;
            var controller = GetController(token.Action, out ILifetimeScope scope);
            object res;
            if (argu != null) res = token.Action.CurrentMethod(controller, new object[] { argu });
            else res = action.CurrentMethod(controller, null);
            var t = typeof(ActionResult<>);
            if (res is ActionResult result)
            {
                var obj = result.GetResult();
                ReplayTo(token.Eargs, 0, "", obj, res.GetType());
            }
            else if (res is Task)
            {

            }
            scope?.Dispose();
        }

        private RpcController GetController(RouteAction action, out ILifetimeScope scope)
        {
            scope = _autofac.BeginLifetimeScope(AUTOFAC_NAME);
            return scope.ResolveOptional(action.ControllerType) as RpcController;
        }
        private void ResetController(Type type, RpcController controller, ILifetimeScope scope)
        {
            scope?.Dispose();
        }
    }
}
