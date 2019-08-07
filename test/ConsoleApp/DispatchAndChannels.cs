using System;
using System.Diagnostics;
using System.Threading;
using YiDian.Soa.Sp;

namespace ConsoleApp
{
    internal class DispatchAndChannels
    {
        class Item
        {
            public static int loop = 1000000;
            int index = 0;
            readonly Action action;
            public Item(Action over)
            {
                action = over;
            }
            public void Add()
            {
                if (Interlocked.Increment(ref index) == loop) action();
            }
            public void Reset()
            {
                index = 0;
            }
        }
        internal static void Test(string[] args)
        {
            if (int.TryParse(args[0], out int res)) Item.loop = res;
            var work_thread = Math.Min(8, Environment.ProcessorCount / 2);
            Console.WriteLine("WorkThread = " + work_thread.ToString());
            var loop = Item.loop;
            var stopwatch = new Stopwatch();
            var item = new Item(() =>
            {
                stopwatch.Stop();
                Console.WriteLine("over take " + stopwatch.ElapsedMilliseconds.ToString());
            });
            Console.WriteLine("Press Any Key Start");
            Console.ReadKey();

            var dispatch = new Dispatcher.DispatchCenter<Item>(DoWork, 1);
            stopwatch.Restart();
            for (var i = 0; i < loop; i++)
            {
                dispatch.Enqueue(item);
            }

            Console.ReadKey();
            item.Reset();

            var channels = new ThreadDispatcher<Item>(DoWork, 1, true);
            stopwatch.Restart();
            for (var i = 0; i < loop; i++)
            {
                channels.QueueWorkItemInternal(item);
            }
        }

        private static void DoWork(Item obj)
        {
            obj.Add();
        }
    }
}