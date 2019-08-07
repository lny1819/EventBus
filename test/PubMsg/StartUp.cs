using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;
using YiDian.EventBus;
using System.Threading.Tasks;
using System.Threading;
using EventModels.es_quote;

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
                 .UseMqRpcClient(Configuration["sysname"])
                 .UseDirectEventBus()
                 .UseTopicEventBus();
#if DEBUG
            soa.AutoCreateAppEvents("es_quote,depthdata");
#endif
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            //var host = sp.GetService<ISoaServiceHost>();

            //var s_quoteinfo = "SEtFWAAAAAAAAABGSFNJAAAAAAAAAAAxOTA3AAAAAAAAAAAAAAAAAAAAAAAATgAAAAAAAAAAAAAAAAAAAAAAAAAAAABOAAAAAAAAAAAAAAAAMjAxOS0wNy0wOSAxMzo0NDo1NC4xMTAAAAAAAACy20AAAAAAALLbQAAAAAAAAAAAAAAAAAC020AAAAAAwH7bQAAAAACAv9tAAAAAAMBu20AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP3xAQAAAAAAAAAAAAAAAAC53AEAAAAAAAAAAAAAAAAAAAAAAACy20AAAAAAALLbQAMAAAAAAAAAAAAAAIB+20AAAAAAgH7bQAAAAABAfttAAAAAAAB+20AAAAAAwH3bQAAAAACAfdtAAAAAAEB920AAAAAAAH3bQAAAAADAfNtAAAAAAIB820AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAACQAAAAAAAAAGAAAAAAAAAAwAAAAAAAAABgAAAAAAAAAWAAAAAAAAAAsAAAAAAAAACgAAAAAAAAAIAAAAAAAAAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwH7bQAAAAABAf9tAAAAAAIB/20AAAAAAwH/bQAAAAAAAgNtAAAAAAECA20AAAAAAgIDbQAAAAADAgNtAAAAAAACB20AAAAAAQIHbQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABQAAAAAAAAAKAAAAAAAAAAgAAAAAAAAACAAAAAAAAAANAAAAAAAAAAkAAAAAAAAADgAAAAAAAAAIAAAAAAAAAAwAAAAAAAAADgAAAAAAAAAAAAAAAAAAADMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAudwBAAAAAAAAAAAAAAAAAK9fi2aUIee/AAAAAACgacD+WjSjDDnyPwAAAAAAAAAAAAAAAAAAAABIS0VYAAAAAAAAAEZIU0kAAAAAAAAAADE5MDcAAAAAAAAAAAAAAAAAAAAAAABOAAAAAAAAAAAAAAAAAAAAAAAAAAAAAE4=";
            //var bytes = Convert.FromBase64String(s_quoteinfo);
            //var info = bytes.ByteToEsQuote();

            //var i = 100000;
            //var sw = Stopwatch.StartNew();
            //for (var x = 0; x < i; x++)
            //{
            //    int size = Marshal.SizeOf(info);
            //    byte[] bytes2 = new byte[size];
            //    IntPtr structPtr = Marshal.AllocHGlobal(size);
            //    Marshal.StructureToPtr(info, structPtr, false);
            //    Marshal.Copy(structPtr, bytes2, 0, size);
            //    Marshal.FreeHGlobal(structPtr);


            //    //var quote = bytes.ByteToEsQuote();
            //    //var ptr = Unsafe.AsPointer(ref quote);
            //    //var new_bytes = new byte[bytes.Length];
            //    //var p = new IntPtr(ptr);
            //    //Marshal.Copy(p, new_bytes, 0, new_bytes.Length);
            //    var quote = bytes2.ByteToEsQuote();
            //}
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);
            //sw.Restart();
            //for (var x = 0; x < i; x++)
            //{
            //    var dp = QuoteHandler.ToDepthData2(ref info);
            //    var write = new WriteStream(1000);
            //    dp.ToBytes(ref write);
            //    var bs = write.GetBytes();
            //    var read = new ReadStream(bs);
            //    dp = new EventModels.depthdata.DepthData();
            //    dp.BytesTo(ref read);
            //}
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);
            //Console.ReadKey();

            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var a = new Exchange() { ExchangeName = "zs", ExchangeNo = "hsi" };
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            var host = sp.GetService<ISoaServiceHost>();
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
                        //topic.Publish(a);
                        //direct.Publish(a);
                        //qps.Add("p");
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
