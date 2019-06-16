using Autofac;
using ConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.Soa.Sp;

namespace ConsoleApp
{

    public class MqA : IntegrationMQEvent
    {
        public string A { get; set; }
        public string B { get; set; }
    }
}

namespace Consumer
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
            services.AddSingleton((e) =>
            {
                var cct = e.GetService<IQpsCounter>();
                return new SleepTaskResult(cct);
            });
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var channels = ThreadChannels.Default;
            var direct = sp.GetService<IDirectEventBus>();
            direct.StartConsumer("test-direct", x =>
            {
                x.Subscribe<MqA, MyHandler>();
            });
        }
    }
    public class MyHandler : IIntegrationEventHandler<MqA>
    {
        public SleepTaskResult TaskResult { get; set; }
        public Task<bool> Handle(MqA @event)
        {
            var cts = TaskSource.Create<bool>(@event);
            var task = cts.Task;
            TaskResult.Push(cts);
            return task;
        }
    }

    public class SleepTaskResult
    {
        readonly ThreadChannels channels;
        readonly IQpsCounter counter;
        public SleepTaskResult(IQpsCounter counter)
        {
            channels = ThreadChannels.Create(12);
            this.counter = counter;
        }
        public void Push(TaskCompletionSource<bool> tcs)
        {
            channels.QueueWorkItemInternal(DoWork, tcs);
        }
        private void DoWork(object obj)
        {
            var item = (TaskCompletionSource<bool>)obj;
            //Thread.Sleep(1);
            item.TrySetResult(true);
            counter.Add("c");
        }
    }
    internal static class TaskSource
    {
        /// <summary>
        /// Create a new TaskCompletion source
        /// </summary>
        /// <typeparam name="T">The type for the created <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
        /// <param name="asyncState">The state for the created <see cref="TaskCompletionSource{TResult}"/>.</param>
        /// <param name="options">The options to apply to the task</param>
        public static TaskCompletionSource<T> Create<T>(object asyncState, TaskCreationOptions options = TaskCreationOptions.None)
            => new TaskCompletionSource<T>(asyncState, options);
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
