using Autofac;
using EventModels.es_quote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
            builder.RegisterAssemblyTypes(curAssembly).Where(e => e.Name.EndsWith("Handler")).PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies).SingleInstance();
            builder.Register((e) =>
            {
                var cct = e.Resolve<IQpsCounter>();
                var tr = new SleepTaskResult(cct);
                var hdl = new MyHandler()
                {
                    TaskResult = tr,
                    Counter = cct,
                    Logger = e.Resolve<ILogger<MyHandler>>()
                };
                return hdl;
            }).SingleInstance();
            soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
                 .UseDirectEventBus(0)
                 .UseTopicEventBus(0);
#if DEBUG
            soa.AutoCreateAppEvents("es_quote");
#endif
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var channels = ThreadChannels.Default;
            var direct = sp.GetRequiredService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            direct.RegisterConsumer("test-direct1", x =>
            {
                x.Subscribe<Exchange, MyHandler>();
            }, queueLength: 10000000, autodel: false, fetchCount: 2000);
            var host = sp.GetService<ISoaServiceHost>();
            Thread.Sleep(3000);
            host.Exit(3);
            //topic.RegisterConsumer("test-topic-1", x =>
            // {
            //     x.Subscribe<MqA, My2Handler>(m => m.A == "a");
            // }, length: 10000000, autodelete: false);
            //topic.RegisterConsumer("test-topic-2", x =>
            //{
            //    x.Subscribe<MqA, My3Handler>("s1.#");
            //}, length: 10000000, autodelete: false);
        }
    }
    public class MyHandler : IEventHandler<Exchange>, IBytesHandler
    {
        readonly Thread thread;

        readonly DataQueue<TaskCompletionSource<bool>>[] his_datas;
        //DataQueue<TaskCompletionSource<bool>> his_data;
        bool quue_flag = false;
        public ILogger<MyHandler> Logger { get; set; }
        public SleepTaskResult TaskResult { get; set; }
        public IQpsCounter Counter { get; set; }
        public MyHandler()
        {
            //his_datas = new DataQueue<TaskCompletionSource<bool>>[2];
            //his_datas[0] = DataQueue<TaskCompletionSource<bool>>.Create();

            //his_data = DataQueue<TaskCompletionSource<bool>>.Create();

            //thread = new Thread(CheckQueueDataHandler);
            //thread.Start();
        }

        //private void CheckQueueDataHandler(object obj)
        //{
        //    int maxsleep = 333;
        //    for (; ; )
        //    {
        //        Thread.Sleep(maxsleep);

        //        //var old_his_data = his_data;
        //        //his_data = DataQueue<TaskCompletionSource<bool>>.Create();

        //        var flag = !quue_flag;
        //        int i = flag ? 0 : 1;
        //        ref var old_his_data = ref his_datas[i];
        //        int x = flag ? 1 : 0;
        //        his_datas[x] = DataQueue<TaskCompletionSource<bool>>.Create();
        //        quue_flag = flag;

        //        if (old_his_data.Length == 0)
        //        {
        //            old_his_data.Reset();
        //            Thread.Sleep(maxsleep);
        //            continue;
        //        }

        //        var span = old_his_data.GetData();


        //        if (span == null)
        //        {
        //            old_his_data.Reset();
        //            Thread.Sleep(maxsleep);
        //            continue;
        //        }
        //        var ider = span.GetEnumerator();
        //        while (ider.MoveNext())
        //        {
        //            ider.Current.SetResult(true);
        //        }
        //        old_his_data.Reset();
        //    }
        //}
        public Task<bool> Handle(Exchange @event)
        {
            var cts = TaskSource.Create<bool>(@event);

            var task = cts.Task;


            int x = quue_flag ? 1 : 0;
            ref var his_data = ref his_datas[x];
            his_data.Enqueue(cts);

            //var newhis = his_data;
            //newhis.Enqueue(cts);

            //TaskResult.Push(cts);
            return task;
        }

        public Task<bool> Handle(string routingKey, byte[] datas)
        {
            var cts = TaskSource.Create<bool>(datas);

            var task = cts.Task;

            //var newhis = his_data;
            //newhis.Enqueue(cts);

            int x = quue_flag ? 1 : 0;
            ref var his_data = ref his_datas[x];
            his_data.Enqueue(cts);
            return task;
        }
    }
    //public class My3Handler : IEventHandler<MqA>, IBytesHandler
    //{
    //    public ILogger<My3Handler> Logger { get; set; }
    //    public SleepTaskResult TaskResult { get; set; }
    //    public IQpsCounter Counter { get; set; }
    //    public Task<bool> Handle(MqA @event)
    //    {
    //        var cts = TaskSource.Create(@event);
    //        var task = cts.Task;
    //        TaskResult.Push(cts);
    //        return task;
    //    }

    //    public ValueTask<bool> Handle(string routingKey, byte[] datas)
    //    {
    //        return new ValueTask<bool>(Task.FromResult(true));
    //    }
    //}
    //public class My2Handler : IEventHandler<MqA>
    //{
    //    public ILogger<My2Handler> Logger { get; set; }
    //    public SleepTaskResult TaskResult { get; set; }
    //    public Task<bool> Handle(MqA @event)
    //    {
    //        var cts = TaskSource.Create(@event);
    //        var task = cts.Task;
    //        TaskResult.Push(cts);
    //        return task;
    //    }
    //}
    public class SleepTaskResult
    {
        readonly ThreadChannels<TaskCompletionSource<bool>> channels;
        readonly IQpsCounter counter;
        public SleepTaskResult(IQpsCounter counter)
        {
            //channels = ThreadChannels.Create(8);
            channels = new ThreadChannels<TaskCompletionSource<bool>>(DoWork, 4);
            this.counter = counter;
        }
        public void Push(TaskCompletionSource<bool> tcs)
        {
            channels.QueueWorkItemInternal(tcs);
        }
        private void DoWork(TaskCompletionSource<bool> obj)
        {
            obj.TrySetResult(true);
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
