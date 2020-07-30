using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.Soa.Sp.Extensions
{
    /// <summary>
    /// MqEventBus扩展方法
    /// </summary>
    public static class MQExtensions
    {
        const string mqsettings = "mqsettings";
        /// <summary>
        /// 注册MQ连接字符串
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname;name=zs</para>
        /// eventsmgr=inmemory
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="action">创建MQ连接，可通过此方法创建多个连接；不同的连接通过name来区分</param>
        /// <param name="eventsManager">消息名称管理器</param>
        /// <param name="retryConnect">MQ断线重连尝试次数</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Action<DefaultMqConnectSource> action, IAppEventsManager eventsManager = null, int retryConnect = 5)
        {
            lock (builder)
            {
                var obj = builder.GetTag(mqsettings);
                if (obj != null) throw new ArgumentException("can not  repeat register the rabbit-mq depend items");
                builder.SetTag(mqsettings, new object());
            }
            var service = builder.Services;
            eventsManager ??= new DefaultEventsManager();
            service.AddSingleton(eventsManager);
            builder.Services.AddSingleton(sp =>
            {
                var subfact = sp.GetService<IEventBusSubManagerFactory>();
                var connSource = new DefaultMqConnectSource(retryConnect, subfact);
                action.Invoke(connSource);
                return connSource;
            });
            builder.Services.AddSingleton<IEventBusSubManagerFactory, InMemorySubFactory>(sp =>
            {
                var subfact = new InMemorySubFactory(eventsManager);
                return subfact;
            });
            builder.Services.AddSingleton(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var l1 = sp.GetService<ILogger<IDirectEventBus>>();
                var l2 = sp.GetService<ILogger<ITopicEventBus>>();
                var l3 = sp.GetService<ILogger<IFanoutEventBus>>();
                var busfact = new EventBusFactory(source, sp, l1, l2, l3);
                return busfact;
            });
            return builder;
        }

        /// <summary>
        /// 使用RabbitMq
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mqConnstr"></param>
        /// <param name="enven_mgr_api"></param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, string mqConnstr, string enven_mgr_api = "")
        {
            if (string.IsNullOrEmpty(enven_mgr_api)) return UseRabbitMq(builder, x => x.Create(mqConnstr), new DefaultEventsManager());
            return UseRabbitMq(builder, x => x.Create(mqConnstr), enven_mgr_api);
        }
        /// <summary>
        /// 使用字符串地址创建RabbitMq
        /// </summary>
        /// <param name="builder">构造器</param>
        /// <param name="mqConnstr">MQ地址</param>
        /// <param name="enven_mgr_api">事件名称管理器地址</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, string mqConnstr, IAppEventsManager enven_mgr_api)
        {
            return UseRabbitMq(builder, x => x.Create(mqConnstr), enven_mgr_api);
        }
        /// <summary>
        /// 多MQ链接和指定enven_mgr_api地址创建RabbitMq
        /// </summary>
        /// <param name="builder">构造器</param>
        /// <param name="action"></param>
        /// <param name="event_mgr_api">只支持WEBAPI地址</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Action<DefaultMqConnectSource> action, string event_mgr_api)
        {
            var addr = string.IsNullOrEmpty(event_mgr_api) ? throw new ArgumentNullException(nameof(IAppEventsManager), "the address of IAppEventsManager can not be empty") : event_mgr_api;
            if (!Uri.TryCreate(event_mgr_api, UriKind.Absolute, out _)) throw new ArgumentException("the api address entered is not vaild", nameof(event_mgr_api));
            var api = new HttpEventsManager(addr);
            return UseRabbitMq(builder, action, api);
        }
        /// <summary>
        /// 创建系统所依赖的消息总线中的消息类型
        /// </summary>
        /// <param name="builder">builder</param>
        /// <param name="all_apps">所依赖的消息系统名称列表，以逗号隔开
        /// <para>为空时，从配置文件中加载键为 dependApps 的值</para>
        /// </param>
        /// <param name="fileDir">创建消息 体的目录
        /// <para>为空时，使用当前系统目录 依赖命令行传入键为-pj_dir的参数</para>
        /// </param>
        /// <returns>builder</returns>
        public static SoaServiceContainerBuilder AutoCreateAppEvents(this SoaServiceContainerBuilder builder, string all_apps = "", string fileDir = "")
        {
            var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            if (string.IsNullOrEmpty(all_apps)) all_apps = config["dependApps"];
            if (string.IsNullOrEmpty(fileDir)) fileDir = builder.Project_Dir;
            if (string.IsNullOrEmpty(fileDir)) throw new ArgumentNullException(nameof(fileDir), "Project Dir is null ,Project_Dir depand the commandline '-pj_dir'");
            var service = builder.Services;
            builder.RegisterRun(new MqEventsLocalBuild());
            //--loadevents -app history,userapi -path /data/his
            var apps = all_apps.Split(',');
            if (apps.Length == 0) throw new ArgumentException("not set event app names");
            var data = new string[5];
            data[0] = "--loadevents";
            data[1] = "-app";
            var s_apps = "";
            for (var i = 0; i < apps.Length; i++)
            {
                s_apps += apps[i];
                if (i != apps.Length - 1)
                {
                    s_apps += ',';
                }
            }
            data[2] = s_apps;
            data[3] = "-path";
            data[4] = fileDir;
            builder.AppendArgs(data);
            return builder;
        }
        /// <summary>
        /// 创建默认的<see cref="IDirectEventBus"/>实现
        /// </summary>
        /// <param name="builder">构造器</param>
        /// <param name="cacheLength">缓存大小</param>
        /// <param name="seralizer">默认使用<see cref="DefaultYDSeralizer"/> UTF8编码</param>
        /// <param name="broker_name">交换机名称</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseDirectEventBus(this SoaServiceContainerBuilder builder, int cacheLength = 0, IEventSeralize seralizer = null, string broker_name = "")
        {
            builder.Services.AddSingleton(sp =>
            {
                var fact = sp.GetService<EventBusFactory>();
                seralizer ??= new DefaultYDSeralizer(Encoding.UTF8);
                var bus = fact.GetDirect(seralizer, "", broker_name, cacheLength);
                return bus;
            });
            return builder;
        }
        /// <summary>
        /// 创建默认的<see cref="ITopicEventBus"/>实现
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="cacheLength"></param>
        /// <param name="seralizer">默认使用<see cref="DefaultYDSeralizer"/> UTF8编码</param>
        /// <param name="broker_name">交换机名称</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseTopicEventBus(this SoaServiceContainerBuilder builder, int cacheLength = 0, IEventSeralize seralizer = null, string broker_name = "")
        {
            builder.Services.AddSingleton(sp =>
            {
                var fact = sp.GetService<EventBusFactory>();
                seralizer ??= new DefaultYDSeralizer(Encoding.UTF8);
                var bus = fact.GetTopic(seralizer, "", broker_name, cacheLength);
                return bus;
            });
            return builder;
        }
        /// <summary>
        /// 创建默认的<see cref="IFanoutEventBus"/>实现
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="cacheLength"></param>
        /// <param name="seralizer">默认使用<see cref="DefaultYDSeralizer"/> UTF8编码</param>
        /// <param name="broker_name">交换机名称</param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseFanoutEventBus(this SoaServiceContainerBuilder builder, int cacheLength = 0, IEventSeralize seralizer = null, string broker_name = "")
        {
            builder.Services.AddSingleton(sp =>
            {
                var fact = sp.GetService<EventBusFactory>();
                seralizer ??= new DefaultYDSeralizer(Encoding.UTF8);
                var bus = fact.GetFanout(seralizer, "", broker_name, cacheLength);
                return bus;
            });
            return builder;
        }
    }
}
