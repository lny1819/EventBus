using System;
using System.Collections.Concurrent;
using System.Threading;


namespace YiDian.EventBus.MQ
{
    class EventObj
    {
        public volatile int ID;
        public AutoResetEvent Event { get; set; }
    }
    public class ThreadChannels<T>
    {
        static readonly object lockobj = new object();
        public Action<Exception> UnCatchedException;
        readonly Thread[] _threads;
        readonly EventObj[] _events;
        ConcurrentQueue<T> queue;
        readonly Action<T> dowork;
        int _limit;
        int _state = 0;
        readonly int _allRun = 0;
        public ThreadChannels(Action<T> action, int limit, bool highlvl = false)
        {
            _limit = Math.Min(limit, Environment.ProcessorCount / 2);
            dowork = action;
            _threads = new Thread[_limit];
            _events = new EventObj[_limit];
            queue = new ConcurrentQueue<T>();
            ExecutionContext.SuppressFlow();
            for (var i = 0; i < _limit; i++)
            {
                _allRun |= (1 << i);
            }
            for (var i = 0; i < _limit; i++)
            {
                _events[i] = new EventObj() { Event = new AutoResetEvent(false), ID = i };
                _state |= (1 << i);
                var thread = CreateThread(i, highlvl);
                _threads[i] = thread;
                thread.Start(i);
            }
            ExecutionContext.RestoreFlow();
        }
        Thread CreateThread(int i, bool highlvl)
        {
            var thread = new Thread(ThreadAction) { IsBackground = true };
            if (highlvl) thread.Priority = ThreadPriority.Highest;
            return thread;
        }

        void ThreadAction(object obj)
        {
            try
            {
                ThreadLoop((int)obj);
            }
            catch (Exception ex)
            {
                UnCatchedException.Invoke(ex);
            }
        }

        void ThreadLoop(int index)
        {
            var thread = _threads[index];
            for (; ; )
            {
                var flag = queue.TryDequeue(out T t);
                if (flag)
                {
                    try
                    {
                        dowork(t);
                    }
                    catch (Exception ex)
                    {
                        UnCatchedException?.Invoke(ex);
                    }
                }
                else
                {
                    ResetThread(index);
                }
            }
        }
        public void QueueWorkItemInternal(T indata)
        {
            queue.Enqueue(indata);
            SetThread();
        }

        private void SetThread()
        {
            if (_state == _allRun) return;
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                return;
            }
            for (var i = 0; i < _limit; i++)
            {
                if (_state == _allRun) return;
                var x = 1 << i;
                var y = _state;
                var r = y | x;
                if (r != y)
                {
                    var flag = Interlocked.CompareExchange(ref _state, r, y) == y;
                    if (flag)
                    {
                        var ev = _events[i];
                        if (ev.ID == 1)
                        {
                            ev.Event.Set();
                        }
                        else
                        {
                            var span = new SpinWait();
                            while (ev.ID != 1)
                            {
                                span.SpinOnce();
                            }
                            ev.Event.Set();
                        }
                    }
                }
            }
            return;
        }

        private void ResetThread(int index)
        {
            for (; ; )
            {
                var i = ~(1 << index);
                var x = _state;
                var j = x & i;
                var flag = Interlocked.CompareExchange(ref _state, j, x) == x;
                if (flag)
                {
                    var ev = _events[index];
                    ev.Event.Reset();
                    ev.ID = 1;
                    ev.Event.WaitOne();
                    ev.ID = 0;
                    break;
                }
            }
        }

        public int GetInWork()
        {
            var y = _state;
            int z = 0;
            for (var i = 0; i < _limit; i++)
            {
                var x = 1 << i;
                var r = y | x;
                if (r == y) z++;
            }
            return z;
        }
    }
}
