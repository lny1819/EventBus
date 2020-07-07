﻿using Autofac;
using EventModels.es_quote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.Rpc;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace RpcTest
{
    internal class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(SoaServiceContainerBuilder soa)
        {
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"]).UseRpc(new RpcServerConfig()
            {
                Delay = 0,
                ControllerCache = 10,
                Fetchout = 5,
                ApplicationId = "test"
            })
            .UseMqRpcClient(Configuration["sysname"]);
#if DEBUG
            soa.AutoCreateAppEvents("es_quote,depthdata,useinfo");
#endif
        }
        public void ConfigContainer(ContainerBuilder builder)
        {
            var curAssembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Controller")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies).SingleInstance();
        }
        public object Start(IServiceProvider sp, string[] args)
        {
            var server = sp.GetService<IRPCServer>();
            var fac = sp.GetService<IRpcClientFactory>();
            var client = fac.Create("test", 10);

            var r1 = client.Call<string>("/home/getid?a=1&b=2");
            Thread.Sleep(3000);
            for (var i = 0; i < 10000; i++)
            {
                r1 = client.Call<string>("/home/getid?a=1&b=2");
                r1 = client.Call<string>("/home/getid?a=1&b=2");
                var r2 = client.Call<Exchange>("/home/GetExchange");
                Thread.Sleep(1);
            }
            return server;
        }
    }
}