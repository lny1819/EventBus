using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    internal class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"]);
        }
        public void Start(IServiceProvider sp, string[] args)
        {

        }
    }
}