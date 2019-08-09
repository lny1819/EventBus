using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
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
            for (; ; )
            {
                DispatchAndChannels.Test(args);
                Console.ReadKey();
            }
        }
    }
}