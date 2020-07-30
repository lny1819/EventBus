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
            var bus1 = sp.GetService<ITopicEventBus>();
            var bus2 = sp.GetService<IFanoutEventBus>();
            var bus4 = sp.GetService<IDirectEventBus>();
            var fact = sp.GetService<EventBusFactory>();
            var bus3 = fact.GetFanout(new DefaultYDSeralizer(Encoding.UTF8), brokerName: "hkex.hsi");
            bus1.RegisterConsumer("q1", x =>
            {
                x.Subscribe<DepthData, Test1Handler>("HKEX.#");
                x.SubscribeBytes<Test1Handler>("test.#");
            }, autodel: true, durable: false, autoAck: false, queueLength: 1000);
            bus2.RegisterConsumer("q2", x =>
            {
                x.SubscribeBytes<Test2Handler>();
                x.Subscribe<DepthData, Test2Handler>();
            }, autodel: true, durable: false, autoAck: false, queueLength: 1000);
            bus3.RegisterConsumer("q3", x =>
            {
                x.SubscribeBytes<Test3Handler>();
                x.Subscribe<DepthData, Test3Handler>();
            }, autodel: true, durable: false, autoAck: false, queueLength: 1000);
            bus4.RegisterConsumer("q4", x =>
            {
                x.SubscribeBytes<Test4Handler>("test.a");
                x.SubscribeBytes<Test4Handler>("test.b");
                x.Subscribe<DepthData, Test4Handler>();
            }, autodel: true, durable: false, autoAck: false, queueLength: 1000);
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
                bus1.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.a", out _);
                bus1.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.b", out _);
                bus1.Publish(dp);
                bus2.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.a", out _);
                bus2.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.b", out _);
                bus2.Publish(dp);
                bus3.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.a", out _);
                bus3.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.b", out _);
                bus3.Publish(dp, out _, true);
                bus4.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.a", out _);
                bus4.PublishBytes(Encoding.UTF8.GetBytes("hello world"), "test.b", out _);
                bus4.Publish(dp, out _, true);
                Console.ReadKey();
            }
        }
    }
}
