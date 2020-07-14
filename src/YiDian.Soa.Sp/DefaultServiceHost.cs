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
        readonly ConsoleLog console_log;
        bool take_build_container = false;
        int exitCode = int.MaxValue;
        public DefaultServiceHost(SoaServiceContainerBuilder builder, string[] args)
        {
            original_args = args;
            _builder = builder;
            waitExit = new AutoResetEvent(false);
            builder.Services.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            console_log = LoadConfigLogConfig();
            ConfigServices();
        }
        class ConsoleLog
        {
            public ConsoleLog()
            {
                Levels = new System.Collections.Generic.Dictionary<string, LogLevel>();
            }
            public bool IncludeScopes { get; set; }
            public LogLevel Default { get; set; }
            public System.Collections.Generic.Dictionary<string, LogLevel> Levels { get; }
        }
        private ConsoleLog LoadConfigLogConfig()
        {
            var log = new ConsoleLog();
            log.IncludeScopes = bool.Parse(config["Logging:IncludeScopes"]);
            var key = "Logging:Console:LogLevel";
            var eum = config.GetSection(key).AsEnumerable().GetEnumerator();
            while (eum.MoveNext())
            {
                var c = eum.Current;
                if (!string.IsNullOrEmpty(c.Value))
                {
                    var name = c.Key.Substring(key.Length + 1);
                    if (name.ToLower() == "default") log.Default = (LogLevel)Enum.Parse(typeof(LogLevel), c.Value);
                    else log.Levels.Add(name, (LogLevel)Enum.Parse(typeof(LogLevel), c.Value));
                }
            }
            return log;
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
                if (exitCode == int.MaxValue)
                {
                    waitExit.Reset();
                    waitExit.WaitOne();
                }
            }
            catch
            {
                throw;
            }
            return exitCode;
        }
        public void Exit(int code)
        {
            exitCode = code;
            waitExit.Set();
        }
        private void Start()
        {
            var appstart = sysstart.GetType().GetMethod("Start");
            if (appstart == null) return;
            appstartResult = appstart.Invoke(sysstart, new object[] { ServicesProvider, original_args });
        }
        private void RegisterLogger()
        {
            _builder.Services.AddLogging(e =>
            {
                e.AddFilter(LoggLelFillter);
                e.AddConsole(x => x.IncludeScopes = console_log.IncludeScopes);
            });
        }

        private bool LoggLelFillter(string provider, string logger, LogLevel lvl)
        {
            if (provider == typeof(Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider).FullName)
            {
                if (console_log.Levels.TryGetValue(logger, out LogLevel set_lvl))
                    return lvl >= set_lvl;
            }
            return lvl >= console_log.Default;
        }
    }
}
