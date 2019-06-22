using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = WithTask();
            var awaiter = task.GetAwaiter();
            awaiter.UnsafeOnCompleted(() =>
            {
                var f = task.IsCompletedSuccessfully;
                Console.WriteLine("2");
            });
            Console.WriteLine("Hello World!");
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
