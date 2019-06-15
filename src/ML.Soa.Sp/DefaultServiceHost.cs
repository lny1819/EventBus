using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using YiDian.EventBus.MQ;
using YiDian.EventBus;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.Soa.Sp
{
    internal class DefaultServiceHost : ISoaServiceHost
    {
        IServiceCollection service;
        static object appstartResult;
        object sysstart;
        readonly string[] _args;
        readonly AutoResetEvent waitExit;
        int exitCode;
        public DefaultServiceHost(ISoaServiceContainerBuilder builder, string[] args)
        {
            _args = args;
            waitExit = new AutoResetEvent(false);
            service = builder.Get<IServiceCollection>();
            if (service == null) service = new ServiceCollection();
            service.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            Init(builder);
            ConfigApps(builder);
        }

        private void ConfigApps(ISoaServiceContainerBuilder builder)
        {
            var start = builder.GetSettings(SoaContent.Startup);
            if (string.IsNullOrEmpty(start)) throw new ArgumentNullException(nameof(start));
            var startup = System.Reflection.Assembly.GetEntryAssembly().GetType(start);

            System.Reflection.ConstructorInfo ci;
            ci = startup.GetConstructor(new Type[] { typeof(IConfiguration) });
            if (ci == null)
            {
                ci = startup.GetConstructor(new Type[] { });
                sysstart = ci.Invoke(new object[0]);
            }
            else sysstart = ci.Invoke(new object[] { Configuration });

            var config = startup.GetMethod("ConfigService");
            var autofac = new ContainerBuilder();
            config.Invoke(sysstart, new object[] { service, autofac });

            autofac.Populate(service);
            var container = autofac.Build();
            ServicesProvider = new AutofacServiceProvider(container);//第三方IOC接管 core内置DI容器
        }

        public event Action<Exception> UnCatchedException;
        private void Init(ISoaServiceContainerBuilder builder)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Configuration = builder.Get<IConfiguration>();
            if (Configuration == null) throw new ArgumentNullException(nameof(Configuration));
            service.AddSingleton(Configuration);

            RegisterBase(builder);

            RegisterLogger(builder);

            RegisterFactory(builder);

            RegisterMqConnection(builder);

            RegisterRpcServerFactory(builder);

            RegisterEventBus(builder);

            RegisterRpcClient(builder);

            RegisterTopicEventBus(builder);

        }

        private void RegisterBase(ISoaServiceContainerBuilder builder)
        {
            var mqstr = builder.GetSettings(SoaContent.MqConnStr);
            var s_enable = Configuration["Logging:Counter:Enabled"];
            bool enabled = true;
            if (s_enable != null && s_enable.ToLower() == "false") enabled = false;
            service.AddSingleton<IQpsCounter>(e =>
            {
                var logger = e.GetService<ILogger<QpsCounter>>();
                var counter = new QpsCounter(logger, enabled);
                return counter;
            });
        }

        public IServiceProvider ServicesProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }
        public int Run(Func<IConfiguration, string> getName, bool background = false)
        {
            var appname = getName(Configuration);
            var logger = ServicesProvider.GetService<ILogger<DefaultServiceHost>>();
            logger.LogWarning($"soa service start with sysname {appname} datetime {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            Start();
            if (!background)
            {
                if (exitCode == 0)
                {
                    waitExit.Reset();
                    waitExit.WaitOne();
                }
                Task.Run(() =>
                {
                    var process = Process.GetCurrentProcess();
                    process.Kill();
                });
            }
            return exitCode;
        }
        public void Exit(int code)
        {
            exitCode = code;
            waitExit.Set();
        }
        static readonly DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        private void Start()
        {
            var appstart = sysstart.GetType().GetMethod("Start");
            if (appstart == null) return;
            appstartResult = appstart.Invoke(sysstart, new object[] { ServicesProvider, _args });
        }
        public void OnException(Exception ex)
        {
            if (UnCatchedException == null)
            {
                var logger = ServicesProvider.GetService<ILogger<DefaultServiceHost>>();
                logger.LogError(ex.Message + Environment.NewLine + ex.ToString());
            }
            else UnCatchedException(ex);
        }
        private void RegisterLogger(ISoaServiceContainerBuilder builder)
        {
            Enum.TryParse(Configuration["Logging:Console:LogLevel:Default"], out LogLevel level);
            service.AddLogging(e =>
            {
                e.AddFilter(m => level <= m);
                e.AddConsole();
            });
            var mqstr = builder.GetSettings(SoaContent.MqConnStr);
            if (string.IsNullOrEmpty(mqstr)) return;
            //service.AddMQLogger();
        }
        private void RegisterMqConnection(ISoaServiceContainerBuilder builder)
        {
            var mqstr = builder.GetSettings(SoaContent.MqConnStr);
            if (string.IsNullOrEmpty(mqstr)) return;
            service.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var factory = CreateConnect(mqstr);
                return new DefaultRabbitMQPersistentConnection(factory, 5);
            })
            .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
        }

        private void RegisterEventBus(ISoaServiceContainerBuilder builder)
        {
            var busloggername = builder.GetSettings(SoaContent.UseDirect);
            if (string.IsNullOrEmpty(busloggername)) return;
            service.AddSingleton<IEventBus, DirectEventBus>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var loggerfact = sp.GetService<ILoggerFactory>();
                var logger = sp.GetService<ILogger<DirectEventBus>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                var counter = sp.GetService<IQpsCounter>();
                var eventbus = new DirectEventBus(logger, iLifetimeScope, conn);
                return eventbus;
            });
        }
        private void RegisterTopicEventBus(ISoaServiceContainerBuilder builder)
        {
            var topicbusname = builder.GetSettings(SoaContent.UseTopic);
            if (!string.IsNullOrEmpty(topicbusname))
                service.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
                {
                    var conn = sp.GetService<IRabbitMQPersistentConnection>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var loggerfact = sp.GetService<ILoggerFactory>();
                    var logger = sp.GetService<ILogger<ITopicEventBus>>();
                    var counter = sp.GetService<IQpsCounter>();
                    return new TopicEventBusMQ(logger, iLifetimeScope, conn);
                });
        }
        private void RegisterRpcServerFactory(ISoaServiceContainerBuilder builder)
        {
            var mqstr = builder.GetSettings(SoaContent.MqConnStr);
            if (string.IsNullOrEmpty(mqstr)) return;
            service.AddSingleton(sp =>
            {
                var creator = builder.Get<IPpcServerCreator>();
                var rabbitMQPersistentConnection = sp.GetService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var counter = sp.GetService<IQpsCounter>();
                var factory = sp.GetService<TaskFactory>();
                var internalCreator = new RpcServerCreator(rabbitMQPersistentConnection, iLifetimeScope, counter, factory, creator);
                return internalCreator;
            });
        }
        private void RegisterFactory(ISoaServiceContainerBuilder builder)
        {
            ThreadPool.SetMinThreads(1000, 1000);
            var settings = builder.Get<ThreadPoolSettings>();
        }
        private void RegisterRpcClient(ISoaServiceContainerBuilder builder)
        {
            var clientName = Configuration["sysname"].ToString();
            if (string.IsNullOrEmpty(clientName)) return;
            var now = DateTime.Now.ToString("MMddHHmmss");
            clientName = "rpcC-" + now + "-" + clientName;
            service.AddSingleton<IMqRpcClientFactory, RpcClientFactory>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var loggerfact = sp.GetService<ILoggerFactory>();
                var qps = sp.GetService<IQpsCounter>();
                var logger = sp.GetService<ILogger<DefaultServiceHost>>();
                var rpc = new RpcClient(rabbitMQPersistentConnection, clientName, logger, qps);
                return new RpcClientFactory(rpc);
            });
        }
        private ConnectionFactory CreateConnect(string connstr)
        {
            string server = "";
            int port = 0;
            string user = "";
            string pwd = "";
            string vhost = "/";
            bool isasync = false;
            var s_arr = connstr.Split(';');
            if (s_arr.Length < 4) throw new ArgumentException("连接字符串格式不正确");
            foreach (var s in s_arr)
            {
                var kv = s.Split('=');
                if (kv[0] == "server")
                {
                    var srs = kv[1].Split(':');
                    server = srs[0];
                    if (srs.Length > 1) port = int.Parse(srs[1]);
                }
                else if (kv[0] == "user") user = kv[1];
                else if (kv[0] == "password") pwd = kv[1];
                else if (kv[0] == "vhost") vhost = kv[1];
                else if (kv[0] == "isasync") isasync = bool.Parse(kv[1]);
            }
            var factory = new ConnectionFactory()
            {
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(3),
                DispatchConsumersAsync = isasync,
                RequestedConnectionTimeout = 30000,
                RequestedHeartbeat = 17,
                HostName = server,
                Password = pwd,
                UserName = user,
                Port = port == 0 ? 5672 : port,
                VirtualHost = vhost
            };
            return factory;
        }
    }
}
