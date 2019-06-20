using Microsoft.Extensions.Configuration;
using System;

namespace YiDian.Soa.Sp.Extensions
{
    public static class SopServiceBuilderExtensions
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
    }
}
