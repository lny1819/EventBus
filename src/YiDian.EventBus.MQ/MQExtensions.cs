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
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname;</para>
        /// eventsmgr=inmemory
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getconnstr"></param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, string mqConnstr, IAppEventsManager eventsManager)
        {
            var service = builder.Services;
            service.AddSingleton(eventsManager);
            builder.RegisterRun(new MqEventsLocalBuild());
            var factory = CreateConnect(mqConnstr);
            var defaultconn = new DefaultRabbitMQPersistentConnection(factory, eventsManager, 5);
            service.AddSingleton<IRabbitMQPersistentConnection>(defaultconn);
            return builder;
        }
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, string mqConnstr, string enven_mgr_api)
        {
            var mgr = new HttpEventsManager(enven_mgr_api);
            return UseRabbitMq(builder, mqConnstr, mgr);
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
        public static SoaServiceContainerBuilder UseDirectEventBus<T>(this SoaServiceContainerBuilder builder, int cacheLength = 0) where T : ISeralize, new()
        {
            builder.Services.AddSingleton<IDirectEventBus, DirectEventBus>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>() ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<DirectEventBus>>();
                var seralize = sp.GetService<T>();
                if (seralize == null) seralize = new T();
                var eventbus = new DirectEventBus(logger, iLifetimeScope, conn, seralize: seralize);
                eventbus.EnableHandlerCache(cacheLength);
                return eventbus;
            });
            return builder;
        }
        public static SoaServiceContainerBuilder UseTopicEventBus<T>(this SoaServiceContainerBuilder builder, int cacheLength = 0) where T : ISeralize, new()
        {
            builder.Services.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>() ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<ITopicEventBus>>();
                var seralize = sp.GetService<T>();
                if (seralize == null) seralize = new T();
                var eventbus = new TopicEventBusMQ(logger, iLifetimeScope, conn, seralize: seralize);
                eventbus.EnableHandlerCache(cacheLength);
                return eventbus;
            });
            return builder;
        }
    }
}
