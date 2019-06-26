using Autofac;
using EventModels.pub_test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utils.Seralize;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

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
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
                 .UseDirectEventBus<JsonSeralizer>(1000)
                 .UseTopicEventBus<JsonSeralizer>(1000)
                 .Services.AddSingleton((e) =>
                 {
                     var cct = e.GetService<IQpsCounter>();
                     return new SleepTaskResult(cct);
                 });
#if DEBUG
            soa.AutoCreateAppEvents("pub_test");
#endif
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var channels = ThreadChannels.Default;
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            direct.StartConsumer("test-direct1", x =>
            {
                x.Subscribe<MqA, MyHandler>();
                x.SubscribeBytes<MqA, MyHandler>();
            }, queueLength: 10000000, durable: false, autodel: false);
            topic.StartConsumer("test-topic-1", x =>
             {
                 x.Subscribe<MqA, My2Handler>(m => m.A == "a");
             }, length: 10000000, durable: false, autodelete: false);
            topic.StartConsumer("test-topic-2", x =>
            {
                x.Subscribe<MqA, MyHandler2>("s1.#");
            }, length: 10000000, durable: false, autodelete: false);
        }
    }
    public class MyHandler : IEventHandler<MqA>, IBytesHandler
    {
        public ILogger<MyHandler> Logger { get; set; }
        public SleepTaskResult TaskResult { get; set; }
        public IQpsCounter Counter { get; set; }
        public Task<bool> Handle(MqA @event)
        {
            Logger.LogInformation("MyHandler  MqA: " + @event.ToJson());
            var cts = TaskSource.Create<bool>(@event);
            var task = cts.Task;
            TaskResult.Push(cts);
            return task;
        }

        public Task<bool> Handle(string routingKey, byte[] datas)
        {
            Logger.LogInformation("MyHandler bytes " + routingKey);
            Counter.Add("c2");
            return Task.FromResult<bool>(true);
        }
    }
    public class MyHandler2 : IEventHandler<MqA>, IBytesHandler
    {
        public ILogger<MyHandler2> Logger { get; set; }
        public SleepTaskResult TaskResult { get; set; }
        public IQpsCounter Counter { get; set; }
        public Task<bool> Handle(MqA @event)
        {
            Logger.LogInformation("MyHandler2 MqA：" + @event.ToJson());
            var cts = TaskSource.Create<bool>(@event);
            var task = cts.Task;
            TaskResult.Push(cts);
            return task;
        }

        public Task<bool> Handle(string routingKey, byte[] datas)
        {
            Logger.LogInformation("MyHandler2 bytes " + routingKey);
            return Task.FromResult<bool>(true);
        }
    }
    public class My2Handler : IEventHandler<MqA>
    {
        public ILogger<My2Handler> Logger { get; set; }
        public SleepTaskResult TaskResult { get; set; }
        public Task<bool> Handle(MqA @event)
        {
            Logger.LogInformation("My2Handler get MqA " + @event.ToJson());
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
