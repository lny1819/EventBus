using Autofac;
using EventModels.es_quote;
using EventModels.MyTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
                 .UseDirectEventBus()
                 .UseTopicEventBus();
#if DEBUG
            soa.AutoCreateAppEvents("es_quote");
#endif
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            MqA xa = new MqA()
            {
                PropertyA = "a",
                PropertyB = "a",
                PropertyLC = new string[2] { "a", "a" },
                Type = MqType.LS,
                PropertyD = new string[2] { "a", "a" },
                PropertyQB = new MqB() { D = new string[] { "a", "a" }, C = "a" },
                Date = DateTime.Now,
                Flag = false,
                QBS = new MqB[]
                {
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" },
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" },
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" }
                }
            };
            CommodityInfo info = new CommodityInfo()
            {
                 CommodityNo="1906"
            };
            Exchange exchange = new Exchange() { ExchangeNo = "hkex", ExchangeName = "港交所" };
            Contract ct = new Contract() { ExchangeNo = "HKEX" };
            var dt = sp.GetService<IDirectEventBus>();
            dt.Publish(info);
            dt.Publish(exchange);
            dt.Publish(ct);
            //HttpEventsManager mgr = new HttpEventsManager("http://192.168.1.220:5000/api/event");
            //var meta = mgr.CreateClassMeta(typeof(MqA), "zs", out List<Type> list);
            //var meta2 = mgr.CreateClassMeta(typeof(MqB), "zs", out list);
            //var load = new MqEventsLocalBuild();
            //var dir = @"F:\XHERO\EventBus\test\PubMsg\test2";
            //load.CreateMainClassFile(dir, "MyTest", meta);
            //load.CreateSeralizeClassFile(dir, "MyTest", meta);
            //load.CreateMainClassFile(dir, "MyTest", meta2);
            //load.CreateSeralizeClassFile(dir, "MyTest", meta2);

            uint size = 1000;
            var stream = new WriteStream(size);
            var size1 = xa.ToBytes(stream);
            var size2 = xa.Size();
            var datas = stream.GetBytes();
            var reads = new ReadStream(datas);
            MqA xb = new MqA();
            xb.BytesTo(reads);

            Console.ReadKey();
            GC.Collect(2);

            var count = 500000;
            var watch = Stopwatch.StartNew();

            //for (var xx = 0; xx < count; xx++)
            //{
            //    var json = xa.ToJson();
            //    var bytes = Encoding.UTF8.GetBytes(json);
            //    var json2 = Encoding.UTF8.GetString(bytes);
            //    json2.JsonTo<MqA>();
            //}
            //watch.Stop();
            //Console.WriteLine("json test:" + watch.ElapsedMilliseconds.ToString());
            //GC.Collect(2);
            //Console.ReadKey();
            watch.Restart();
            for (var xx = 0; xx < count; xx++)
            {
                size = 1000;
                stream = new WriteStream(size);
                xa.ToBytes(stream);
                datas = stream.GetBytes();
                reads = new ReadStream(datas);
                xb = new MqA();
                xb.BytesTo(reads);
            }
            Console.WriteLine("mystream test:" + watch.ElapsedMilliseconds.ToString());
            GC.Collect(2);
            Console.ReadKey();

            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            res = eventsMgr.VaildityTest("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            var a = new MqA() { PropertyA = "a", PropertyB = "b2" };
            var b = new MqA() { PropertyA = "b", PropertyB = "b1" };
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            var ps = int.Parse(Configuration["ps"]);
            var type = Configuration["type"];
            var sleep = int.Parse(Configuration["sleep"]);
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = ps;
                    for (var j = 0; j < i; j++)
                    {
                        //topic.PublishPrefix(a, "s1");
                        //topic.Publish(a);
                        //direct.Publish(b);
                        //direct.Publish(a);
                        //direct.Publish(b);
                        qps.Add("p");
                        if (type == "direct")
                        {
                            direct.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-where")
                        {
                            topic.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-pre")
                        {
                            topic.PublishPrefix(a, "s1");
                            qps.Add("i");
                        }
                    }
                    Thread.Sleep(sleep);
                }
            });
        }
    }
}
