﻿using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
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
        public unsafe void Start(IServiceProvider sp, string[] args)
        {
            var host = sp.GetService<ISoaServiceHost>();
            host.Exit(999);
            ////var s_quoteinfo = "SEtFWAAAAAAAAABGSFNJAAAAAAAAAAAxOTA3AAAAAAAAAAAAAAAAAAAAAAAATgAAAAAAAAAAAAAAAAAAAAAAAAAAAABOAAAAAAAAAAAAAAAAMjAxOS0wNy0wOSAxMzo0NDo1NC4xMTAAAAAAAACy20AAAAAAALLbQAAAAAAAAAAAAAAAAAC020AAAAAAwH7bQAAAAACAv9tAAAAAAMBu20AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP3xAQAAAAAAAAAAAAAAAAC53AEAAAAAAAAAAAAAAAAAAAAAAACy20AAAAAAALLbQAMAAAAAAAAAAAAAAIB+20AAAAAAgH7bQAAAAABAfttAAAAAAAB+20AAAAAAwH3bQAAAAACAfdtAAAAAAEB920AAAAAAAH3bQAAAAADAfNtAAAAAAIB820AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAACQAAAAAAAAAGAAAAAAAAAAwAAAAAAAAABgAAAAAAAAAWAAAAAAAAAAsAAAAAAAAACgAAAAAAAAAIAAAAAAAAAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwH7bQAAAAABAf9tAAAAAAIB/20AAAAAAwH/bQAAAAAAAgNtAAAAAAECA20AAAAAAgIDbQAAAAADAgNtAAAAAAACB20AAAAAAQIHbQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABQAAAAAAAAAKAAAAAAAAAAgAAAAAAAAACAAAAAAAAAANAAAAAAAAAAkAAAAAAAAADgAAAAAAAAAIAAAAAAAAAAwAAAAAAAAADgAAAAAAAAAAAAAAAAAAADMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAudwBAAAAAAAAAAAAAAAAAK9fi2aUIee/AAAAAACgacD+WjSjDDnyPwAAAAAAAAAAAAAAAAAAAABIS0VYAAAAAAAAAEZIU0kAAAAAAAAAADE5MDcAAAAAAAAAAAAAAAAAAAAAAABOAAAAAAAAAAAAAAAAAAAAAAAAAAAAAE4=";
            ////var bytes = Convert.FromBase64String(s_quoteinfo);
            ////var info = bytes.ByteToEsQuote();

            ////var i = 100000;
            ////var sw = Stopwatch.StartNew();
            ////for (var x = 0; x < i; x++)
            ////{
            ////    var quote = bytes.ByteToEsQuote();
            ////    var ptr = Unsafe.AsPointer(ref quote);
            ////    var new_bytes = new byte[bytes.Length];
            ////    var p = new IntPtr(ptr);
            ////    Marshal.Copy(p, new_bytes, 0, new_bytes.Length);
            ////    quote = new_bytes.ByteToEsQuote();
            ////}
            ////sw.Stop();
            ////Console.WriteLine(sw.ElapsedMilliseconds);
            ////sw.Restart();
            ////for (var x = 0; x < i; x++)
            ////{
            ////    var dp = QuoteHandler.ToDepthData2(ref info);
            ////    var write = new WriteStream(1000);
            ////    dp.ToBytes(ref write);
            ////    var bs = write.GetBytes();
            ////    var read = new ReadStream(bs);
            ////    dp = new EventModels.depthdata.DepthData();
            ////    dp.BytesTo(ref read);
            ////}
            ////sw.Stop();
            ////Console.WriteLine(sw.ElapsedMilliseconds);
            ////Console.ReadKey();
            //var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            //var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            //if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            //res = eventsMgr.VaildityTest("pub_test", "1.2");
            //if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            //var a = new MqA() { PropertyA = "a", PropertyB = "b2" };
            //var b = new MqA() { PropertyA = "b", PropertyB = "b1" };
            //var direct = sp.GetService<IDirectEventBus>();
            //var topic = sp.GetService<ITopicEventBus>();
            //var qps = sp.GetService<IQpsCounter>();
            //var ps = int.Parse(Configuration["ps"]);
            //var type = Configuration["type"];
            //var sleep = int.Parse(Configuration["sleep"]);
            //Task.Run(() =>
            //{
            //    for (; ; )
            //    {
            //        var i = ps;
            //        for (var j = 0; j < i; j++)
            //        {
            //            //topic.PublishPrefix(a, "s1");
            //            //topic.Publish(a);
            //            //direct.Publish(b);
            //            //direct.Publish(a);
            //            //direct.Publish(b);
            //            qps.Add("p");
            //            if (type == "direct")
            //            {
            //                direct.Publish(a);
            //                qps.Add("i");
            //            }
            //            else if (type == "top-where")
            //            {
            //                topic.Publish(a);
            //                qps.Add("i");
            //            }
            //            else if (type == "top-pre")
            //            {
            //                topic.PublishPrefix(a, "s1");
            //                qps.Add("i");
            //            }
            //        }
            //        Thread.Sleep(sleep);
            //    }
            //});
        }
    }
}
