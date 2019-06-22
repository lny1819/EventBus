using Autofac;
using ConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{

    public class MqA : IntegrationMQEvent
    {
        [KeyIndex(0)]
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
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
            var curAssembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Handler")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            soa.UseRabbitMq(Configuration["mqconnstr"], null)
                 .UseDirectEventBus<MySeralize>(1000)
                 .UseTopicEventBus<MySeralize>(1000)
                 .Services.AddSingleton((e) =>
                 {
                    var cct = e.GetService<IQpsCounter>();
                    return new SleepTaskResult(cct);
                 });
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var channels = ThreadChannels.Default;
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            //direct.StartConsumer("test-direct", x =>
            //{
            //    x.Subscribe<MqA, MyHandler>();
            //    x.SubscribeDynamic<MyHandler>("MqA");
            //}, queueLength: 10000000, durable: false, autodel: true);
            topic.StartConsumer("test-direct-2", x =>
             {
                 x.Subscribe<MqA, My2Handler>(m => m.A == "a");
             }, length: 10000000, durable: false, autodelete: true);
            //topic.StartConsumer("test-direct-3", x =>
            //{
            //    x.Subscribe<MqA, My2Handler>("zs");
            //    x.Subscribe<MyHandler>("#.MqA");
            //}, length: 10000000, durable: false, autodelete: true);
        }
    }
    public class MyHandler : IIntegrationEventHandler<MqA>, IDynamicBytesHandler
    {
        public SleepTaskResult TaskResult { get; set; }
        public IQpsCounter Counter { get; set; }
        public Task<bool> Handle(MqA @event)
        {
            var cts = TaskSource.Create<bool>(@event);
            var task = cts.Task;
            TaskResult.Push(cts);
            return task;
        }

        public Task<bool> Handle(string routingKey, byte[] datas)
        {
            Counter.Add("c2");
            return Task.FromResult<bool>(true);
        }
    }
    public class My2Handler : IIntegrationEventHandler<MqA>
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
            //channels = ThreadChannels.Create(8);
            channels = ThreadChannels.Default;
            this.counter = counter;
        }
        public void Push(TaskCompletionSource<bool> tcs)
        {
            channels.QueueWorkItemInternal(DoWork, tcs);
        }
        private void DoWork(object obj)
        {
            var i = channels.GetInWork();
            var item = (TaskCompletionSource<bool>)obj;
            item.TrySetResult(true);
            counter.Add("c1");
            counter.Set("w", i);
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
