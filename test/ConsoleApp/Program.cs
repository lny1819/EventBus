using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        //static void Main(string[] args)
        //{
        //    ServiceHost.CreateBuilder(args)
        //         .ConfigApp(e => e.AddJsonFile("appsettings.json"))
        //         .UserStartUp<StartUp>()
        //         .Build()
        //         .Run(e => e["sysname"]);
        //}

        static Action Tick = null;
        static void Main()
        {
            Start();
            while (true)
            {
                if (Tick != null) Tick();
                Thread.Sleep(1);
            }
        }
        static async void Start()
        {
            Console.WriteLine("执行开始");
            for (int i = 1; i <= 4; ++i)
            {
                Console.WriteLine($"第{i}次，时间：{DateTime.Now.ToString("HH:mm:ss")} - 线程号：{Thread.CurrentThread.ManagedThreadId}");
                await TaskEx.Delay(1000);
            }
            Console.WriteLine("执行完成");
        }
        class TaskEx
        {
            public static MyDelay Delay(int ms) => new MyDelay(ms);
        }
        class MyDelay : INotifyCompletion
        {
            private readonly DateTime _start;
            private readonly int _ms;
            public MyDelay(int ms)
            {
                _start = DateTime.Now;
                _ms = ms;
            }
            internal MyDelay GetAwaiter() => this;
            public void OnCompleted(Action continuation)
            {
                Tick += Check;
                void Check()
                {
                    Console.Write("*");
                    var a = DateTime.Now;
                    if ((a - _start).TotalMilliseconds > _ms)
                    {
                        continuation();
                        Tick -= Check;
                    }
                }
            }
            public void GetResult() { }
            public bool IsCompleted => false;
        }


    }
}
