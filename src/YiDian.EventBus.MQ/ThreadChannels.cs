using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("ML.Soa.Sp")]

namespace YiDian.EventBus
{
    public class ThreadChannels<T>
    {
        static readonly object lockobj = new object();
        static ThreadChannels<T> _current;
        public static IQpsCounter Counter { get; set; }
        public static ThreadChannels<T> Current
        {
            get
            {
                if (_current == null)
                {
                    lock (lockobj)
                    {
                        if (_current == null)
                            _current = new ThreadChannels<T>(Environment.ProcessorCount);
                    }
                }
                return _current;
            }
        }

        private static void LoopError(Exception obj)
        {
            Console.WriteLine("---------------LoopError---------------");
            Console.WriteLine(obj.ToString());
        }
        private static void UserError(Exception obj)
        {
            Console.WriteLine("---------------UserError---------------");
            Console.WriteLine(obj.ToString());
        }
        Channel<Tuple<Action<T>, T>> actions;
        public event Action<Exception> UnCatchedException;
        readonly ConcurrentStack<Thread> _freeThread;
        int _limit;

        public void SetThreadLimit(int limit)
        {
            if (limit <= _limit) return;
            var old = _limit;
            _limit = limit;
            ExecutionContext.SuppressFlow();
            for (var i = old; i < limit; i++)
            {
                CreateThread(false);
            }
            ExecutionContext.RestoreFlow();
        }
        internal ThreadChannels(int limit, bool highlvl = false)
        {
            ExecutionContext.SuppressFlow();
            IsUsed = true;
            _limit = limit;
            _freeThread = new ConcurrentStack<Thread>();
            actions = Channel.CreateBounded<Tuple<Action<T>, T>>(20000);
            for (var i = 0; i < limit; i++)
            {
                CreateThread(highlvl);
            }
            ExecutionContext.RestoreFlow();
        }
        void CreateThread(bool highlvl)
        {
            var thread = new Thread(ThreadAction) { IsBackground = true };
            if (highlvl) thread.Priority = ThreadPriority.Highest;
            thread.Start(thread);
        }

        void ThreadAction(object obj)
        {
            try
            {
                ThreadLoop(obj as Thread);
            }
            catch (Exception ex)
            {
                (UnCatchedException ?? LoopError).Invoke(ex);
            }
        }

        void ThreadLoop(Thread obj)
        {
            for (; ; )
            {
                var flag = actions.Reader.TryRead(out Tuple<Action<T>, T> action);
                if (flag)
                {
                    try
                    {
                        action.Item1(action.Item2);
                    }
                    catch (Exception ex)
                    {
                        (UnCatchedException ?? UserError).Invoke(ex);
                    }
                }
                else
                {
                    lock (obj)
                    {
                        _freeThread.Push(obj);
                        Monitor.Wait(obj, Timeout.Infinite);
                    }
                }
            }
        }

        public void QueueWorkItemInternal(Action<T> action, T indata = default(T))
        {
            while (!actions.Writer.TryWrite(new Tuple<Action<T>, T>(action, indata)))
            {
                Counter?.Add("tdF");
                Thread.Sleep(1);
            }
            Counter?.Add("tdS");
            var flag = _freeThread.TryPop(out Thread thread);
            if (!flag) return;
            lock (thread)
            {
                Monitor.PulseAll(thread);
            }
        }
        public int WorkThreads { get { return _limit - _freeThread.Count; } }
        public bool IsUsed { get; }
    }
}
