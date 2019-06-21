using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
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
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Func<IConfiguration, string> mqConnstr)
        {
            var factory = CreateConnect(mqConnstr(builder.Config));
            var defaultconn = new DefaultRabbitMQPersistentConnection(factory, 5);
            var service = builder.Services;
            service.AddSingleton<IRabbitMQPersistentConnection>(defaultconn);
            var evensMgr = new InMemoryAppEventsManager();
            service.AddSingleton<IAppEventsManager>(evensMgr);
            builder.RegisterRun(new MqEventsLoalBuild());
            return builder;
        }
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Func<IRabbitMQPersistentConnection> getFactory)
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
        public static IServiceCollection UseDirectEventBus<T>(this IServiceCollection service, int cacheLength = 0) where T : ISeralize, new()
        {
            service.AddSingleton<IDirectEventBus, DirectEventBus>(sp =>
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
            return service;
        }
        public static IServiceCollection UseTopicEventBus<T>(this IServiceCollection service, int cacheLength = 0) where T : ISeralize, new()
        {
            service.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
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
            return service;
        }
    }

    internal class MqEventsLoalBuild : IAppRun
    {
        public string Name { get; private set; }
        public void Run(ISoaServiceHost host, string name, string[] args)
        {
            //--loadevents -app history,userapi -path /data/his
            Name = name;
            for (var i = 0; i < args.Length; i++)
            {
                var list = new List<AppMetas>();
                if (args[i].ToLower() == "--loadevents")
                {
                    string appname = "";
                    string path = "";
                    var n1 = args[i + 1];
                    var n2 = args[i + 3];
                    var v1 = args[i + 2];
                    var v2 = args[i + 4];
                    var arr1 = new string[] { n1, v1 };
                    var arr2 = new string[] { n2, v2 };
                    var arr = new string[2][] { arr1, arr2 };
                    foreach (var n in arr)
                    {
                        if (n[0] == "-app") appname = n[1];
                        else if (n[0] == "-path") path = n[1];
                    }
                    var sp = host.ServicesProvider;
                    var mgr = sp.GetService<IAppEventsManager>();
                    var appnames = appname.Split(',');
                    foreach (var app in appnames)
                    {
                        list.Add(mgr.GetAppEventTypes(app));
                    }
                    ReloadEvents(list, path);
                    host.Exit(0);
                }
            }
        }

        private void ReloadEvents(List<AppMetas> list, string path)
        {

        }
    }
}
