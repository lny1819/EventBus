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
using System.Collections.Generic;

namespace YiDian.Soa.Sp
{
    internal class DefaultServiceHost : ISoaServiceHost
    {
        IServiceCollection service;
        static object appstartResult;
        object sysstart;
        readonly string[] _args;
        readonly AutoResetEvent waitExit;
        readonly List<IAppRun> run_list;
        int exitCode = 0;
        int state = 0;
        public DefaultServiceHost(SoaServiceContainerBuilder builder, string[] args)
        {
            run_list = builder.GetAllAppRun();
            _args = args;
            waitExit = new AutoResetEvent(false);
            service = builder.Services;
            Configuration = builder.Config;
            service.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            Init(builder);
            ConfigApps(builder);
        }

        private void ConfigApps(SoaServiceContainerBuilder builder)
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
        private void Init(SoaServiceContainerBuilder builder)
        {
            Console.OutputEncoding = Encoding.UTF8;

            service.AddSingleton(builder.Config);

            RegisterBase(builder);

            RegisterLogger(builder);

        }

        private void RegisterBase(SoaServiceContainerBuilder builder)
        {
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
            if (appname == null) throw new ArgumentNullException("Run->GetAppName");
            var logger = ServicesProvider.GetService<ILogger<DefaultServiceHost>>();
            logger.LogWarning($"soa service start with sysname {appname} datetime {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            try
            {
                foreach (var run in run_list)
                {
                    run.Run(this, appname, _args);
                }
                if (state != 0) return exitCode;
                Start();
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
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
            if (Interlocked.CompareExchange(ref state, 0, 1) == 0)
            {
                exitCode = code;
                waitExit.Set();
            }
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
        void OnException(Exception ex)
        {
            if (UnCatchedException == null)
            {
                var logger = ServicesProvider.GetService<ILogger<DefaultServiceHost>>();
                logger.LogError(ex.Message + Environment.NewLine + ex.ToString());
            }
            else UnCatchedException(ex);
        }
        private void RegisterLogger(SoaServiceContainerBuilder builder)
        {
            Enum.TryParse(Configuration["Logging:Console:LogLevel:Default"], out LogLevel level);
            service.AddLogging(e =>
            {
                e.AddFilter(m => level <= m);
                e.AddConsole();
            });
        }
    }
}
