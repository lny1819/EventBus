using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.DefaultConnection;
using YiDian.EventBus.MQ.Rpc;

namespace YiDian.Soa.Sp.Extensions
{
    public static class RPCServerExtensions
    {
        /// <summary>
        /// 在指定的MQ上创建一个RPC服务器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="seralize"></param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRpc(this SoaServiceContainerBuilder builder, RpcServerConfig config)
        {
            builder.Services.AddSingleton<IRPCServer, RPCServer>(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var conn = source.Get(config.MQName) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var loggerfact = sp.GetService<ILoggerFactory>();
                var qps = sp.GetService<IQpsCounter>();
                var scope = sp.GetService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<RPCServer>>();
                var rpc = new RPCServer(conn, logger, config, scope, qps);
                return rpc;
            });
            return builder;
        }
        /// <summary>
        /// 在指定的MQ上创建RPC客户端工厂
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientName"></param>
        /// <param name="mqname"></param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseMqRpcClient(this SoaServiceContainerBuilder builder, string clientName, string mqname = "")
        {
            if (string.IsNullOrEmpty(clientName)) return builder;
            var now = DateTime.Now.ToString("MMddHHmmss");
            clientName = "rpcC-" + now + "-" + clientName;
            builder.Services.AddSingleton<IRpcClientFactory, RpcClientFactory>(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var conn = source.Get(mqname) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var loggerfact = sp.GetService<ILoggerFactory>();
                var qps = sp.GetService<IQpsCounter>();
                var logger = sp.GetService<ILogger<IMQRpcClient>>();
                var rpc = new MQRpcClientBase(conn, clientName, logger, qps);
                return new RpcClientFactory(rpc);
            });
            return builder;
        }
    }
}
