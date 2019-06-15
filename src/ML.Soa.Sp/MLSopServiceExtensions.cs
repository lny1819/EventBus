using Microsoft.Extensions.Configuration;
using System;
using YiDian.EventBus;

namespace YiDian.Soa.Sp
{
    public static class MLSopServiceBuilderExtensions
    {
        public static ISoaServiceContainerBuilder UserStartUp<TStartup>(this ISoaServiceContainerBuilder builder) where TStartup : class
        {
            var t = typeof(TStartup);
            builder.SetSettings(SoaContent.Startup, t.FullName);
            return builder;
        }
        public static ISoaServiceContainerBuilder ConfigApp(this ISoaServiceContainerBuilder builder, Action<IConfigurationBuilder> configaction)
        {
            var configbuilder = new ConfigurationBuilder();
            configaction(configbuilder);
            var config = configbuilder.Build();
            builder.Add<IConfiguration>(config);
            return builder;
        }
        /// <summary>
        /// 注册MQ连接字符串
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getconnstr"></param>
        /// <returns></returns>
        public static ISoaServiceContainerBuilder RegisterMqConnection(this ISoaServiceContainerBuilder builder, Func<IConfiguration, string> getconnstr)
        {
            var config = builder.Get<IConfiguration>();
            var connstr = getconnstr(config);
            builder.SetSettings(SoaContent.MqConnStr, connstr);
            return builder;
        }
        public static ISoaServiceContainerBuilder UseRpcServer(this ISoaServiceContainerBuilder builder, IPpcServerCreator serverCreator)
        {
            builder.Add(serverCreator);
            return builder;
        }
        public static ISoaServiceContainerBuilder UseEventbus<T>(this ISoaServiceContainerBuilder builder)
        {
            builder.SetSettings(SoaContent.UseDirect, typeof(T).FullName);
            return builder;
        }
        public static ISoaServiceContainerBuilder UseTopicEventBus<T>(this ISoaServiceContainerBuilder builder)
        {
            builder.SetSettings(SoaContent.UseTopic, typeof(T).FullName);
            return builder;
        }
        public static ISoaServiceContainerBuilder UseConcurrencyFactory(this ISoaServiceContainerBuilder builder, Func<IConfiguration, int> concurrency)
        {
            var config = builder.Get<IConfiguration>();
            var i = concurrency(config);
            var settings = new ThreadPoolSettings() { PubMode = false, TaskLimit = i };
            builder.Add(settings);
            return builder;
        }

        public static ISoaServiceContainerBuilder UseConcurrencyFactory(this ISoaServiceContainerBuilder builder, Func<IConfiguration, ThreadPoolSettings> concurrency)
        {
            var config = builder.Get<IConfiguration>();
            var settings = concurrency(config);
            builder.Add(settings);
            return builder;
        }
    }
}
