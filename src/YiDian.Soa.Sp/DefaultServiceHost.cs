using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;

namespace YiDian.Soa.Sp
{
    internal class DefaultServiceHost : ISoaServiceHost
    {
        static object appstartResult;
        object sysstart;
        readonly AutoResetEvent waitExit;
        readonly SoaServiceContainerBuilder _builder;
        readonly IConfiguration config;
        readonly string[] original_args;
        bool take_build_container = false;
        int exitCode = 0;
        public DefaultServiceHost(SoaServiceContainerBuilder builder, string[] args)
        {
            original_args = args;
            _builder = builder;
            waitExit = new AutoResetEvent(false);
            builder.Services.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            ConfigServices();
        }

        private void ConfigServices()
        {
            Console.OutputEncoding = Encoding.UTF8;
            RegisterBase();
            RegisterLogger();

            var startup = _builder.StartUp;
            if (sysstart == null)
            {
                var sp = _builder.Services.BuildServiceProvider();
                sysstart = sp.GetService(_builder.StartUp);
            }
            var config = startup.GetMethod("ConfigService");
            config.Invoke(sysstart, new object[] { _builder });
        }
        public IServiceProvider ConfigContainerBuilder(ContainerBuilder container)
        {
            take_build_container = true;
            var startup = _builder.StartUp;

            if (sysstart == null)
            {
                var sp = _builder.Services.BuildServiceProvider();
                sysstart = sp.GetService(_builder.StartUp);
            }
            var config = startup.GetMethod("ConfigContainer");
            if (container == null)
            {
                container = new ContainerBuilder();
                container.Populate(_builder.Services);
                config.Invoke(sysstart, new object[] { container });
                ServicesProvider = new AutofacServiceProvider(container.Build());//第三方IOC接管 core内置DI容器
            }
            else config.Invoke(sysstart, new object[] { container });
            return ServicesProvider;
        }

        private void RegisterBase()
        {
            _builder.Services.AddSingleton<IQpsCounter>(e =>
            {
                var logger = e.GetService<ILogger<QpsCounter>>();
                var counter = new QpsCounter(logger, true);
                return counter;
            });
        }

        public IServiceProvider ServicesProvider { get; private set; }

        public int Run(Func<IConfiguration, string> getName, bool background, IServiceProvider provider)
        {
            if (!take_build_container) ConfigContainerBuilder(null);
            else ServicesProvider = provider ?? throw new Exception("should set provider");
            var appname = getName(ServicesProvider.GetService<IConfiguration>());
            if (appname == null) throw new ArgumentNullException("Run->GetAppName");
            var logger = ServicesProvider.GetService<ILogger<ISoaServiceHost>>();
            logger.LogWarning($"soa service start with sysname {appname} datetime {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            try
            {
                foreach (var run in _builder.GetAllAppRun())
                {
                    run.Run(this, appname, _builder.GetArgs());
                }
                if (background)
                {
                    Start();
                    return 0;
                }
                Start();
                waitExit.Reset();
                waitExit.WaitOne();
            }
            catch
            {
                throw;
            }
            return exitCode;
        }
        public void Exit(int code)
        {
            var logger = ServicesProvider.GetService<ILogger<ISoaServiceHost>>();
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
            appstartResult = appstart.Invoke(sysstart, new object[] { ServicesProvider, original_args });
        }
        private void RegisterLogger()
        {
            Enum.TryParse(config["Logging:Console:LogLevel:Default"], out LogLevel level);
            _builder.Services.AddLogging(e =>
            {
                e.AddFilter(m => level <= m);
                e.AddConsole();
            });
        }
    }
}
