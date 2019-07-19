using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using YiDian.Soa.Sp;

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
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            DispatchAndChannels.Test(args);
            Console.WriteLine();
        }
    }
}