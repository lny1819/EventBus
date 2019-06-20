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

            service.AddSingleton(builder.Config);

            RegisterBase(builder);

            RegisterLogger(builder);

        }

        private void RegisterBase(SopServiceContainerBuilder builder)
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
            try
            {
                var appstart = sysstart.GetType().GetMethod("Start");
                if (appstart == null) return;
                appstartResult = appstart.Invoke(sysstart, new object[] { ServicesProvider, _args });
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
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
        private void RegisterLogger(SopServiceContainerBuilder builder)
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
