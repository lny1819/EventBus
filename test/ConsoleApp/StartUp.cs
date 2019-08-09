using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
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
    class SB : IConfigurationProvider
    {
        Hashtable jsonobj;
        public void LoadFromFile(string path)
        {
            if (!System.IO.Path.IsPathRooted(path)) path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            var str_json = System.IO.File.ReadAllText(path);
            jsonobj = (Hashtable)JsonString.Unpack(str_json);
        }
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
            Hashtable curObj = jsonobj;
            var itor = key.Split(':').GetEnumerator();
            value = null;
            while (itor.MoveNext())
            {
                var k = (string)itor.Current;
                var curObj2 = (Hashtable)curObj[k];
                if (curObj2 == null)
                {
                    return curObj.ContainsKey(k);
                }
                value = curObj2.ToString();
            }
            return true;
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
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
        }
        public void Start(IServiceProvider sp, string[] args)
        {
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
