using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
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
#if DEBUG
            //soa.AutoCreateAppEvents("quote_es");
#endif
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var hb = System.Text.Encoding.UTF8.GetBytes("helloworld");
            var ms = new MemoryStream(hb);
            ms.Write(hb);
            ms.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[10];
            ms.Read(bytes, 0, 10);
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