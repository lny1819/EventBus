using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus;

namespace ConsoleApp
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(IServiceCollection services, ContainerBuilder builder)
        {
            var curAssembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Handler")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var direct = sp.GetService<IDirectEventBus>();
            direct.StartConsumer("test-direct", x =>
            {
                x.Subscribe<MqA, MyHandler>();
            });
            var qps = sp.GetService<IQpsCounter>();
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = 3000;
                    for (var j = 0; j < i; j++)
                    {
                        var a = new MqA() { A = "a", B = "b" };
                        direct.Publish(a);
                        qps.Add("i");
                    }
                    Thread.Sleep(300);
                }
            });
        }
    }
    public class MqA : IntegrationMQEvent
    {
        public string A { get; set; }
        public string B { get; set; }
    }
    public class MyHandler : IIntegrationEventHandler<MqA>
    {
        public IQpsCounter Counter { get; set; }
        public ValueTask<bool> Handle(MqA @event)
        {
            Counter.Add("m");
            //return new ValueTask<bool>(true);
            var t = Task.Run(() =>
             {
                 Thread.Sleep(1);
                 return true;
             });
            return new ValueTask<bool>(t);
        }
    }
    public class MyAwait : ICriticalNotifyCompletion, IAsyncResult
    {
        public void OnCompleted(Action continuation)
        {
            continuation.Invoke();
        }
        public void CbkFunc(IAsyncResult asyncResult)
        {

        }
        public void UnsafeOnCompleted(Action continuation)
        {
            Thread.Sleep(3000);
            _isCp = true;
            continuation.BeginInvoke(CbkFunc, null);
        }
        public MyAwait GetAwaiter()
        {
            return this;
        }
        public int GetResult()
        {
            return 2;
        }
        private bool _isCp = false;
        public bool IsCompleted
        {
            get
            {
                return _isCp;
            }
            private set
            {
                _isCp = value;
            }
        }

        public object AsyncState => throw new NotImplementedException();

        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public bool CompletedSynchronously => throw new NotImplementedException();
    }
}
