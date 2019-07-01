using Autofac;
using EventModels.es_quote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Utils.Seralize;
using YiDian.EventBus;
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
            var curAssembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Handler")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            soa.UseRabbitMq(Configuration["mqconnstr"], new JsonSeralizer()).UseTopicEventBus();
#if DEBUG
            //soa.AutoCreateAppEvents("es_quote");
#endif
        }
        public struct SSAA
        {
            public string SA { get; set; }
            public int P { get; set; }
            public void SetP(int i)
            {
                P += i;
            }
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            DataQueue<SSAA>.Reload(200, 100);
            var e = DataQueue<SSAA>.Create();
            while (true)
            {
                for (var i = 0; i < 200; i++)
                {
                    var x = new SSAA() { P = i + 1, SA = "zs" };
                    e.Enqueue(x);
                }
                var b = e.GetData();
                var dd = b.GetEnumerator();
                while (dd.MoveNext())
                {
                    var x = dd.Current;
                    x.SetP(1);
                }
            }

            var top = sp.GetService<ITopicEventBus>();
            top.RegisterConsumer("rec_quote_bytes", (x) =>
            {
                x.SubscribeBytes<QuoteBytes, BytesHandler>("#.QuoteBytes");
            }, 100, 3000, true, false, true);
            //B b = new B() { ZA = new A() { D = "zs" }, AV = 2 };
            //C c = new C() { AB = b, SC = "hello" };
            //var json = c.ToJson();
            //var json = "{\"metaInfos\":[{\"attr\":null,\"name\":\"MqA\",\"properties\":[{\"name\":\"A\",\"type\":\"string\",\"attr\":{\"attrType\":1,\"value\":\"0\"}},{\"name\":\"B\",\"type\":\"string\",\"attr\":null},{\"name\":\"ErrorCode\",\"type\":\"Int32\",\"attr\":null},{\"name\":\"ErrorMsg\",\"type\":\"string\",\"attr\":null}]}],\"version\":\"1.0\",\"name\":\"pub_test\"}";
            //var d = JsonString.Unpack(json);
            //var m = HttpEventsManager.ToMetas(json);
            Console.WriteLine();
        }
    }
}