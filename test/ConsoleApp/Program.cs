using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YiDian.EventBus.MQ;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        class XA
        {
            public string Name { get; set; }
            public List<Tuple<string, string>> List { get; set; }
        }
        static void Main(string[] args)
        {
            //List<Tuple<string, string>> list = new List<Tuple<string, string>>
            //{
            //    new Tuple<string, string>("zs", "ls"),
            //    new Tuple<string, string>("zs2", "ls2"),
            //    new Tuple<string, string>("zs3", "ls3"),
            //};
            //XA xa = new XA
            //{
            //    List = list,
            //    Name = "zs"
            //};
            //var arr = xa.List.ToJson();
            //var obj = JsonString.Unpack(arr);
            //Console.WriteLine();
            ServiceHost.CreateBuilder(args)
                 .ConfigApp(e => e.AddJsonFile("appsettings.json"))
                 .UserStartUp<StartUp>()
                 .Build()
                 .Run(e => e["sysname"]);

            //var task = WithTask();
            //var awaiter = task.GetAwaiter();
            //awaiter.UnsafeOnCompleted(() =>
            //{
            //    var f = task.IsCompletedSuccessfully;
            //    Console.WriteLine("2");
            //});
            //Console.WriteLine("Hello World!");
            //Console.ReadKey();
        }

        private static void WriteProperty(Type t)
        {
            foreach (var p in t.GetProperties())
            {
                Console.Write(p.Name);
                Console.Write(" ");
                Console.Write(p.PropertyType.Name);
                Console.WriteLine();
            }
        }

        static Task<int> WithTask()
        {
            return Task.Delay(1000).ContinueWith<int>(x =>
            {
                throw new ArgumentException();
            });
        }
    }
}
