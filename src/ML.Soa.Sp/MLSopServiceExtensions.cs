using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace YiDian.Soa.Sp
{
    public static class MLSopServiceBuilderExtensions
    {
        public static SopServiceContainerBuilder UserStartUp<TStartup>(this SopServiceContainerBuilder builder) where TStartup : class
        {
            var t = typeof(TStartup);
            builder.StartUp = t;
            return builder;
        }
        public static SopServiceContainerBuilder ConfigApp(this SopServiceContainerBuilder builder, Action<IConfigurationBuilder> configaction)
        {
            var configbuilder = new ConfigurationBuilder();
            configaction(configbuilder);
            var config = configbuilder.Build();
            builder.Config = config;
            return builder;
        }
        /// <summary>
        /// 注册MQ连接字符串
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getconnstr"></param>
        /// <returns></returns>
        public static SopServiceContainerBuilder RegisterMqConnection(this SopServiceContainerBuilder builder, Func<IConfiguration, string> getconnstr)
        {
            var config = builder.Get<IConfiguration>();
            var connstr = getconnstr(config);
            builder.SetSettings(SoaContent.MqConnStr, connstr);
            return builder;
        }
        public static SopServiceContainerBuilder UseEventbus<T>(this SopServiceContainerBuilder builder)
        {
            builder.SetSettings(SoaContent.UseDirect, typeof(T).FullName);
            return builder;
        }
        public static SopServiceContainerBuilder UseTopicEventBus<T>(this SopServiceContainerBuilder builder)
        {
            builder.SetSettings(SoaContent.UseTopic, typeof(T).FullName);
            return builder;
        }
        public static SopServiceContainerBuilder UseConcurrencyFactory(this SopServiceContainerBuilder builder, Func<IConfiguration, int> concurrency)
        {
            var config = builder.Get<IConfiguration>();
            var i = concurrency(config);
            var settings = new ThreadPoolSettings() { PubMode = false, TaskLimit = i };
            builder.Add(settings);
            return builder;
        }

        public static SopServiceContainerBuilder UseConcurrencyFactory(this SopServiceContainerBuilder builder, Func<IConfiguration, ThreadPoolSettings> concurrency)
        {
            var config = builder.Get<IConfiguration>();
            var settings = concurrency(config);
            builder.Add(settings);
            return builder;
        }
    }
}
