﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace YiDian.Soa.Sp.Extensions
{
    public static class SoaServiceBuilderExtensions
    {
        public static SoaServiceContainerBuilder UserStartUp<TStartup>(this SoaServiceContainerBuilder builder) where TStartup : class
        {
            var t = typeof(TStartup);
            builder.Services.AddScoped(t);
            builder.StartUp = t;
            return builder;
        }
        public static SoaServiceContainerBuilder ConfigApp(this SoaServiceContainerBuilder builder, Action<IConfigurationBuilder> configaction)
        {
            var configbuilder = new ConfigurationBuilder();
            configaction(configbuilder);
            var config = configbuilder.Build();
            builder.Services.AddSingleton<IConfiguration>(config);
            return builder;
        }
    }
}
