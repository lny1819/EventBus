using Autofac;
using EventModels.userinfo;
using Microsoft.Extensions.Configuration;
using System;
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
        public void ConfigService(SoaServiceContainerBuilder soa)
        {
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
                 .UseMqRpcClient(Configuration["sysname"])
                 .UseDirectEventBus()
                 .UseTopicEventBus();
#if DEBUG
            soa.AutoCreateAppEvents("userinfo");
#endif
        }
        public void ConfigContainer(ContainerBuilder builder)
        {
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
            var ws = new WriteStream(2000);
            var info = new RspUseAction
            {
                Data = new RspUserOrderInfo()
                {
                    Action = OrderActType.DELETE,
                    Commodity = "HSI",
                    ContractId = "2006",
                    Exchange = "HKEX",
                    FillSize = 5,
                    LocalOrderNo = "OA23411",
                    Price = 4.5,
                    ServiceNo = "SO98",
                    Size = 5,
                    State = OrderState.Accept
                },
                IsLast = true,
                SessionId = 0
            };
            info.ToBytes(ref ws);
            var bs = ws.GetBytes();
            var rs = new ReadStream(bs);
            var info2 = rs.ReadEventObj<RspUseAction>();
        }
    }
}
