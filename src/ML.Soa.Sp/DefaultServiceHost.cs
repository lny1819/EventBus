using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

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
        public DefaultServiceHost(SopServiceContainerBuilder builder, string[] args)
        {
            _args = args;
            waitExit = new AutoResetEvent(false);
            service = builder.Services;
            if (service == null) service = new ServiceCollection();
            service.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            Init(builder);
            ConfigApps(builder);
        }

        private void ConfigApps(SopServiceContainerBuilder builder)
        {
            var startup = builder.StartUp;

            System.Reflection.ConstructorInfo ci = startup.GetConstructor(new Type[] { typeof(IConfiguration) });
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
        private void Init(SopServiceContainerBuilder builder)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var sp = builder.Services.GetServiceProvider();
            Configuration = builder.Services.GetService<IConfiguration>();
            if (Configuration == null) throw new ArgumentNullException(nameof(Configuration));
            service.AddSingleton(Configuration);

            RegisterBase(builder);

            RegisterLogger(builder);

            RegisterFactory(builder);

            RegisterEventBus(builder);

            RegisterTopicEventBus(builder);

        }

        private void RegisterBase(SopServiceContainerBuilder builder)
        {
            RegisterMqConnection(builder);

            service.AddSingleton<IQpsCounter>(e =>
            {
                var logger = e.GetService<ILogger<QpsCounter>>();
                var counter = new QpsCounter(logger, true);
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
                Task.Delay(1000).ContinueWith((x) =>
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
        private void RegisterLogger(SopServiceContainerBuilder builder)
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
        private void RegisterMqConnection(SopServiceContainerBuilder builder)
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

        private void RegisterEventBus(SopServiceContainerBuilder builder)
        {
            var busloggername = builder.GetSettings(SoaContent.UseDirect);
            if (string.IsNullOrEmpty(busloggername)) return;
            service.AddSingleton<IDirectEventBus, DirectEventBus>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var logger = sp.GetService<ILogger<DirectEventBus>>();
                var seralize = new SeralizeTest();
                var eventbus = new DirectEventBus(logger, iLifetimeScope, conn, seralize: seralize);
                return eventbus;
            });
        }
        private void RegisterTopicEventBus(SopServiceContainerBuilder builder)
        {
            var topicbusname = builder.GetSettings(SoaContent.UseTopic);
            if (!string.IsNullOrEmpty(topicbusname))
                service.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
                {
                    var conn = sp.GetService<IRabbitMQPersistentConnection>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var logger = sp.GetService<ILogger<ITopicEventBus>>();
                    var seralize = new SeralizeTest();
                    return new TopicEventBusMQ(logger, iLifetimeScope, conn, seralize: seralize);
                });
        }
        private void RegisterFactory(SopServiceContainerBuilder builder)
        {
            ThreadPool.SetMinThreads(100, 100);
            var settings = builder.Get<ThreadPoolSettings>();
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
