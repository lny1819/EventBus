using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
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
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var fact = sp.GetService<EventBusFactory>();
            var bus1 = sp.GetService<ITopicEventBus>();
            var bus2 = sp.GetService<IFanoutEventBus>();
            var bus3 = fact.GetFanout(new DefaultYDSeralizer(Encoding.UTF8), brokerName: "hkex.hsi");
            bus1.RegisterConsumer("rec_topic_test", x =>
            {
            }, autodelete: true, autoAck: true);
        }
    }
}
