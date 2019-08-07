using Microsoft.Extensions.Logging;
using YiDian.EventBus.MQ.Route;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using Autofac;
using YiDian.EventBus.MQ.Abstractions;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using YiDian.Soa.Sp;

namespace YiDian.EventBus.MQ
{
    internal class RPCServer
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
        internal RPCServer(IRabbitMQPersistentConnection conn, ILogger logger, RpcServerConfig config, ILifetimeScope autofac, IQpsCounter qps)
        {
            config.ApplicationId = config.ApplicationId.ToLower();
            RoutingTables.LoadControlers(config.ApplicationId);

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
            var stopwatch = new Stopwatch();
            stopwatch.Restart();
            var action = RoutingTables.Route(ea.RoutingKey, Configs.ApplicationId, out string msg);
            if (action == null)
            {
                _factory.StartNew(() =>
               {
                   _logger.LogError($"未找到匹配方法 请求地址：{ea.RoutingKey} 错误消息：{msg}");
                   var replayData = new ResponseBase
                   {
                       ServerState = 401,
                       ServerMsg = msg
                   };
                   ReplayTo(ea, replayData);
                   stopwatch.Stop();
               });
                return;
            }
            var clienttime = UnixTimestampToDate(BitConverter.ToInt64(ea.Body, 0));
            if (Math.Abs((DateTime.Now - clienttime).TotalSeconds) > 10)
            {
                stopwatch.Stop();
                _logger.LogWarning($"请求已超时 请求 {ea.RoutingKey} 耗时 {stopwatch.ElapsedMilliseconds.ToString()}ms");
                return;
            }
            _factory.StartNew(() => Excute(action, ea, stopwatch)).ContinueWith(x =>
            {
                if (x.Status == TaskStatus.Faulted)
                {
                    _logger?.LogError(x.Exception.ToString());
                    stopwatch.Stop();
                    _logger.LogError($" 请求出错 {ea.RoutingKey} 耗时 {stopwatch.ElapsedMilliseconds.ToString()}ms");
                }
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReplayTo(BasicDeliverEventArgs ea, ResponseBase replayData)
        {
            var replyTo = ea.BasicProperties.ReplyTo;
            var replyTold = ea.BasicProperties.CorrelationId;
            var replyProps = _pubChannel.CreateBasicProperties();
            replyProps.CorrelationId = replyTold;
            var data = replayData.SeralizeAndGetBytes(Configs.Encode);
            _pubChannel.BasicPublish("", routingKey: replyTo, basicProperties: replyProps, body: data);
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
            public Stopwatch Stopwatch { get; set; }
        }
        private void Excute(RouteAction route_action, BasicDeliverEventArgs ea, Stopwatch stopwatch)
        {
            object invoke_data = null;
            if (route_action.InArgumentType != null)
            {
                var data = ea.Body;
                invoke_data = data.DeseralizeBytes(8, data.Length - 8, route_action.InArgumentType, Configs.Encode);
            }
            var token = new Token() { Stopwatch = stopwatch, Action = route_action, Data = invoke_data, Eargs = ea };
            var action = token.Action;
            var argu = token.Data;
            var controller = GetController(token.Action, out ILifetimeScope scope);
            var replayData = new ResponseBase();
            if (argu != null)
            {
                var res = token.Action.CurrentMethod(controller, new object[] { argu }) as ActionResult;
                replayData.Data = res.GetResult();
            }
            else
            {
                var res = action.CurrentMethod(controller, null) as ActionResult;
                replayData.Data = res.GetResult();
            }
            ReplayTo(token.Eargs, replayData);
            ResetController(action.ControllerType, controller, scope);
            _qps.Add("task");
            token.Stopwatch.Stop();
            _logger.LogInformation($"请求 {ea.RoutingKey} 耗时 {token.Stopwatch.ElapsedMilliseconds.ToString()}ms");
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
