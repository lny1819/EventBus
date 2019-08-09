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
                //DispatchAndChannels.Test(args);
                var configbuilder = new ConfigurationBuilder();
                configbuilder.Add(new SA());
                var config = configbuilder.Build();
                var value = config["zs"];
                DictTest.Test();
                Console.ReadKey();
            }
        }
    }
    class SBc : IChangeToken, IDisposable
    {
        public bool HasChanged { get; }

        public bool ActiveChangeCallbacks { get; }

        public void Dispose()
        {

        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return this;
        }
    }
    class SA : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SB();
        }
    }
    class SB : IConfigurationProvider
    {
        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            return new List<string>() { "zs", "ls" };
        }

        public IChangeToken GetReloadToken()
        {
            return new SBc();
        }

        public void Load()
        {

        }

        public void Set(string key, string value)
        {
            Console.WriteLine("Set");
        }

        public bool TryGet(string key, out string value)
        {
            value = "test";
            Console.WriteLine("TryGet");
            return true;
        }
    }
}