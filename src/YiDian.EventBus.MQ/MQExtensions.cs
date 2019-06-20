using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.Soa.Sp.Extensions
{
    public static class MQExtensions
    {
        /// <summary>
        /// 注册MQ连接字符串
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getconnstr"></param>
        /// <returns></returns>
        public static SopServiceContainerBuilder UseRabbitMq(this SopServiceContainerBuilder builder, string mqConnstr)
        {
            var factory = CreateConnect(mqConnstr);
            var defaultconn = new DefaultRabbitMQPersistentConnection(factory, 5);
            var service = builder.Services;
            service.AddSingleton(defaultconn);
            return builder;
        }
        public static SopServiceContainerBuilder UseRabbitMq(this SopServiceContainerBuilder builder, Func<IRabbitMQPersistentConnection> getFactory)
        {
            var defaultconn = getFactory() ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var service = builder.Services;
            service.AddSingleton(defaultconn);
            return builder;
        }
        private static ConnectionFactory CreateConnect(string connstr)
        {
            string server = "";
            int port = 0;
            string user = "";
            string pwd = "";
            string vhost = "/";
            bool isasync = false;
            var s_arr = connstr.Split(';');
            if (s_arr.Length < 4) throw new ArgumentException("连接字符串格式不正确");
            foreach (var s in s_arr)
            {
                var kv = s.Split('=');
                if (kv[0] == "server")
                {
                    var srs = kv[1].Split(':');
                    server = srs[0];
                    if (srs.Length > 1) port = int.Parse(srs[1]);
                }
                else if (kv[0] == "user") user = kv[1];
                else if (kv[0] == "password") pwd = kv[1];
                else if (kv[0] == "vhost") vhost = kv[1];
                else if (kv[0] == "isasync") isasync = bool.Parse(kv[1]);
            }
            var factory = new ConnectionFactory()
            {
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(3),
                DispatchConsumersAsync = isasync,
                RequestedConnectionTimeout = 30000,
                RequestedHeartbeat = 17,
                HostName = server,
                Password = pwd,
                UserName = user,
                Port = port == 0 ? 5672 : port,
                VirtualHost = vhost
            };
            return factory;
        }
        public static SopServiceContainerBuilder UseDirectEventBus(this SopServiceContainerBuilder builder, ISeralize seralize)
        {
            var service = builder.Services;
            service.AddSingleton<IDirectEventBus, DirectEventBus>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>() ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<DirectEventBus>>();
                var eventbus = new DirectEventBus(logger, iLifetimeScope, conn, seralize: seralize);
                return eventbus;
            });
            return builder;
        }
        public static SopServiceContainerBuilder UseTopicEventBus(this SopServiceContainerBuilder builder, ISeralize seralize)
        {
            var service = builder.Services;
            service.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>() ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<ITopicEventBus>>();
                return new TopicEventBusMQ(logger, iLifetimeScope, conn, seralize: seralize);
            });
            return builder;
        }
    }
}
