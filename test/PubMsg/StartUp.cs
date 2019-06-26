using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utils.Seralize;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
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
                 .UseDirectEventBus<JsonSeralizer>(1000)
                 .UseTopicEventBus<JsonSeralizer>(1000);
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            var a = new MqA() { A = "a", B = "b2" };
            var b = new MqA() { A = "b", B = "b1" };
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            var ps = int.Parse(Configuration["ps"]);
            var type = Configuration["type"];
            var sleep = int.Parse(Configuration["sleep"]);
            var channel = ThreadChannels.Default;
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = ps;
                    for (var j = 0; j < i; j++)
                    {
                        topic.PublishPrefix(a, "s1");
                        topic.Publish(a);
                        direct.Publish(b);
                        direct.Publish(a);
                        direct.Publish(b);
                        //qps.Add("i");
                        //channel.QueueWorkItemInternal(x =>
                        //{
                        //    if (type == "direct")
                        //        direct.Publish(a);
                        //    else if (type == "top-where")
                        //        topic.Publish(a);
                        //    else if (type == "top-pre")
                        //        topic.PublishPrefix(a, "s1");
                        //});
                    }
                    Thread.Sleep(sleep);
                }
            });
        }
    }
    public class MqA : IMQEvent
    {
        [KeyIndex(0)]
        public string A { get; set; }
        public string B { get; set; }
        public MqB QB { get; set; }
        public List<string> LC { get; set; }
        public string[] D { get; set; }
        public MqType Type { get; set; }
    }
    public enum MqType
    {
        ZS = 1,
        LS = 2
    }
    public class MqB
    {
        public string C { get; set; }
        public string[] D { get; set; }
    }
}
