using Autofac;
using EventModels.depthdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Text;
using System.Threading;
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
        public void ConfigService(SoaServiceContainerBuilder soa)
        {
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
                 .UseMqRpcClient(Configuration["sysname"])
                 .UseDirectEventBus()
                 .UseFanoutEventBus()
                 .UseTopicEventBus();
#if DEBUG
            soa.AutoCreateAppEvents("depthdata");
#endif
        }
        public void ConfigContainer(ContainerBuilder builder)
        {
            var curAssembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Handler")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies).SingleInstance();
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var fact = sp.GetService<EventBusFactory>();
            var bus1 = sp.GetService<ITopicEventBus>();
            var bus2 = sp.GetService<IFanoutEventBus>();
            var bus3 = fact.GetFanout(new DefaultYDSeralizer(Encoding.UTF8), brokerName: "hkex.hsi");
            bus1.RegisterConsumer("rec_topic_test", x =>
            {
                x.Subscribe<DepthData, Test1Handler>("HKEX.#");
            }, autodelete: true);
            bus2.RegisterConsumer("rec_fanout_test", x =>
            {
                x.Subscribe<DepthData, Test2Handler>();
            });
            bus3.RegisterConsumer("rec_fanout_test", x =>
            {
                x.Subscribe<DepthData, Test3Handler>();
            });
            var dp = new DepthData()
            {
                ExchangeID = "HKEX",
                CommodityNo = "HSI",
                InstrumentID = "2007",
                LastPrice = 12817,
                Volume = 5
            };
            for (; ; )
            {
                bus1.Publish(dp);
                bus2.Publish(dp);
                bus3.Publish(dp, out _, true);
                Thread.Sleep(3000);
            }
        }
    }
}
