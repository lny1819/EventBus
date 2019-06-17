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
        static Task<int> WithTask()
        {
            return Task.Delay(1000).ContinueWith<int>(x =>
            {
                throw new ArgumentException();
            });
        }
    }
}
