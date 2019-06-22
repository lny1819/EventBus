using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using YiDian.EventBus;
using YiDian.EventBus.MQ;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var mgr = new HttpEventsManager("http://www.baidu.com");
            var appmeta = new AppMetas() { Name = "quote", Version = "1.0" };
            var meta = new ClassMeta()
            {
                Name = "CA"
            };
            meta.Properties.Add(new PropertyMetaInfo() { Name = "p1", Type = PropertyMetaInfo.P_String });
            appmeta.MetaInfos.Add(meta);
            var json = appmeta.ToJson();
            var m2 = mgr.ToMetas(json);
            //var task = WithTask();
            //var awaiter = task.GetAwaiter();
            //awaiter.UnsafeOnCompleted(() =>
            //{
            //    var f = task.IsCompletedSuccessfully;
            //    Console.WriteLine("2");
            //});
            //Console.WriteLine("Hello World!");
            Console.ReadKey();
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
