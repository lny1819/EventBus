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
        static object appstartResult;
        object sysstart;
        readonly AutoResetEvent waitExit;
        readonly SoaServiceContainerBuilder _builder;
        readonly string[] original_args;
        int exitCode = 0;
        int state = 0;
        public DefaultServiceHost(SoaServiceContainerBuilder builder, string[] args)
        {
            original_args = args;
            _builder = builder;
            waitExit = new AutoResetEvent(false);
            var service = builder.Services;
            service.AddSingleton<ISoaServiceHost, DefaultServiceHost>((s) => this);
            Init();
            ConfigApps();
        }

        private void ConfigApps()
        {
            var startup = _builder.StartUp;

            System.Reflection.ConstructorInfo ci = startup.GetConstructor(new Type[] { typeof(IConfiguration) });
            if (ci == null)
            {
                ci = startup.GetConstructor(new Type[] { });
                sysstart = ci.Invoke(new object[0]);
            }
            else sysstart = ci.Invoke(new object[] { Configuration });

            var config = startup.GetMethod("ConfigService");
            var autofac = new ContainerBuilder();
            config.Invoke(sysstart, new object[] { _builder, autofac });

            autofac.Populate(_builder.Services);
            var container = autofac.Build();
            ServicesProvider = new AutofacServiceProvider(container);//第三方IOC接管 core内置DI容器
        }

        public event Action<Exception> UnCatchedException;
        private void Init()
        {
            Console.OutputEncoding = Encoding.UTF8;

            RegisterBase();

            RegisterLogger();

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

        public IConfiguration Configuration { get { return _builder.Config; } }
        public int Run(Func<IConfiguration, string> getName, bool background = false)
        {
            var appname = getName(Configuration);
            if (appname == null) throw new ArgumentNullException("Run->GetAppName");
            var logger = ServicesProvider.GetService<ILogger<DefaultServiceHost>>();
            logger.LogWarning($"soa service start with sysname {appname} datetime {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            try
            {
                foreach (var run in _builder.GetAllAppRun())
                {
                    run.Run(this, appname, _builder.GetArgs());
                }
                Task.Run(() => Start());
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
            appstartResult = appstart.Invoke(sysstart, new object[] { ServicesProvider, original_args });
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
        private void RegisterLogger()
        {
            Enum.TryParse(Configuration["Logging:Console:LogLevel:Default"], out LogLevel level);
            _builder.Services.AddLogging(e =>
            {
                e.AddFilter(m => level <= m);
                e.AddConsole();
            });
        }
    }
}
