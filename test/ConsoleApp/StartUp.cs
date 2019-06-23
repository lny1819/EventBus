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
            //B b = new B() { ZA = new A() { D = "zs" }, AV = 2 };
            //C c = new C() { AB = b, SC = "hello" };
            //var json = c.ToJson();
            //var json = "{\"metaInfos\":[{\"attr\":null,\"name\":\"MqA\",\"properties\":[{\"name\":\"A\",\"type\":\"string\",\"attr\":{\"attrType\":1,\"value\":\"0\"}},{\"name\":\"B\",\"type\":\"string\",\"attr\":null},{\"name\":\"ErrorCode\",\"type\":\"Int32\",\"attr\":null},{\"name\":\"ErrorMsg\",\"type\":\"string\",\"attr\":null}]}],\"version\":\"1.0\",\"name\":\"pub_test\"}";
            //var d = JsonString.Unpack(json);
            //var m = HttpEventsManager.ToMetas(json);
            Console.WriteLine();
        }
        public class A
        {
            public string D { get; set; }
        }
        public class B
        {
            public int AV { get; set; }
            public A ZA { get; set; }
        }
        public class C
        {
            public B AB { get; set; }
            public String SC { get; set; }
        }
    }
}