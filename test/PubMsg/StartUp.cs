using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using YiDian.EventBus;
using YiDian.EventBus.MQ;

namespace ConsoleApp
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }
        readonly ConcurrentQueue<MqA> __processQueue = new ConcurrentQueue<MqA>();
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(IServiceCollection services, ContainerBuilder builder)
        {
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            var a = new MqA() { A = "a", B = "b" };
            var direct = sp.GetService<IDirectEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = 6000;
                    for (var j = 0; j < i; j++)
                    {
                        qps.Add("i");
                        __processQueue.Enqueue(a);
                        StartProcess(direct);
                        CheckQueue();
                    }
                    Thread.Sleep(300);
                }
            });
        }

        private void CheckQueue()
        {
            if (__processQueue.Count > 20000)
                Thread.Sleep(1);
            else if (__processQueue.Count > 30000)
            {
                Thread.Sleep(30);
            }
            else if (__processQueue.Count > 40000)
            {
                Thread.Sleep(1000);
            }
        }

        const int ProcessStop = 0;
        const int ProcessStart = 1;

        private  int process_state = ProcessStop;
        private  void StartProcess(IDirectEventBus direct)
        {
            if (Interlocked.CompareExchange(ref process_state, ProcessStart, ProcessStop) != ProcessStop) return;

            Task.Run(() =>
            {
                for (; ; )
                {
                    var flag = __processQueue.TryDequeue(out MqA item);
                    if (!flag)
                    {
                        Interlocked.Exchange(ref process_state, ProcessStop);
                        break;
                    }
                    direct.Publish(item);
                }
            });
        }
    }
    public class MqA : IntegrationMQEvent
    {
        public string A { get; set; }
        public string B { get; set; }
    }
}
