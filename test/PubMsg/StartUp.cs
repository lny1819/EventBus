using Autofac;
using EventModels.es_quote;
using EventModels.userinfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
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
            soa.AutoCreateAppEvents("userinfo,es_quote");
#endif
        }
        public void ConfigContainer(ContainerBuilder builder)
        {
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var s_quoteinfo = "agAAAAMCBQQDBQcCAgAAAAoBAAAACwEAAAAMAAAAAA4AAAAABwAAAAAAAAAACAAAAAAAAAAACZqZmZmZmbk/AAUAAABUT0NPTQEAAAAAAwMAAABKU1YEAAAAAAUAAAAABgAAAAANAAAAAA==";
            var s_quoteinfo2 = "TgAAAAMCAQQCBQcBAgAAAAQAAAAAAAAAAAUAAAAAAAAAAAADAAAAQ01FAgIAAABKWQMEAAAAMjMwMwYAAAAABwAAAAAIAAAAAAkAAAAAAAAAU00EAAAAAAUAAAAABgAAAAANAAAAAAAAAA==";
            var bytes = Convert.FromBase64String(s_quoteinfo);
            var bytes2 = Convert.FromBase64String(s_quoteinfo2);
            var read = new ReadStream(bytes);
            var commodity = new CommodityInfo();
            commodity.BytesTo(read);
            var ws = new WriteStream(2000);
            commodity.ToBytes(ws);
            var a = ws.GetBytes();
            var b = new ReadStream(a);
            commodity = new CommodityInfo();
            commodity.BytesTo(b);
            //var info = new RspUseAction
            //{
            //    Data = new RspUserOrderInfo()
            //    {
            //        Action = OrderActType.DELETE,
            //        Commodity = "HSI",
            //        ContractId = "2006",
            //        Exchange = "HKEX",
            //        FillSize = 5,
            //        LocalOrderNo = "OA23411",
            //        Price = 4.5,
            //        ServiceNo = "SO98",
            //        Size = 5,
            //        State = OrderState.Accept
            //    },
            //    IsLast = true,
            //    SessionId = 0
            //};
            //info.ToBytes(ref ws);
            //var bs = ws.GetBytes();
            //var rs = new ReadStream(bs);
            //var info2 = rs.ReadEventObj<RspUseAction>();
        }
    }
}
