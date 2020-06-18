using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using YiDian.Soa.Sp;


namespace Microsoft.Extensions.Configuration
{
    public static class JsonExtensions
    {
        public static IConfigurationBuilder AddJson(this IConfigurationBuilder builder, string path)
        {
            var file = new SA(path);
            builder.Add(file);
            return builder;
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
        string _path;
        public SA(string path)
        {
            _path = path;
        }
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var sb = new SB();
            sb.LoadFromFile(_path);
            return sb;
        }
    }
}

namespace ConsoleApp
{
    internal class StartUp
    {
        public IConfiguration Configuration { get; }

        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(IServiceCollection services)
        {
        }
        public void ConfigBuilder(ContainerBuilder services)
        {
        }
        class Persion
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime Date { get; set; }
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var v = System.Text.Json.JsonSerializer.Serialize(new Persion() { Age = 1, Name = "zs", Date = DateTime.Now });
            for (; ; )
            {
                //DispatchAndChannels.Test(args);
                var configbuilder = new ConfigurationBuilder();
                configbuilder.AddJson("appsettings.json");
                var config = configbuilder.Build();
                var value = config["ww"];
                config.GetChildren();
                var sec = config.GetSection("ww");
                var sec2 = config.GetSection("zs");
                var sec3 = config.GetSection("ww:zs");
                DictTest.Test();
                Console.ReadKey();
            }
        }
    }
}
