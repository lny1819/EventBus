using System;
using System.Collections.Concurrent;
using System.Threading;


namespace YiDian.Soa.Sp
{
    class EventObj
    {
        public volatile int ID;
        public int Index;
        public AutoResetEvent Event { get; set; }
        public Thread Thread;
        public bool IsDisposed;
    }
    public class ThreadDispatcher<T> : IDisposable
    {
        static readonly object lockobj = new object();
        public Action<Exception> UnCatchedException;
        readonly EventObj[] _events;
        ConcurrentQueue<T> queue;
        readonly Action<T> dowork;
        readonly int _limit;
        int _state = 0;
        int _set_sync = 0;
        readonly int _allRun = 0;
        public ThreadDispatcher(Action<T> action, int limit = 0, bool highlvl = true)
        {
            _limit = limit == 0 ? Math.Min(8, Environment.ProcessorCount / 2) : limit;
            dowork = action ?? throw new ArgumentNullException(nameof(action));
            _events = new EventObj[_limit];
            queue = new ConcurrentQueue<T>();
            ExecutionContext.SuppressFlow();
            for (var i = 0; i < _limit; i++)
            {
                _allRun |= (1 << i);
            }
            for (var i = 0; i < _limit; i++)
            {
                _events[i] = new EventObj() { Event = new AutoResetEvent(true), Index = i };
                _state |= (1 << i);
                var thread = CreateThread(i, highlvl);
                _events[i].Thread = thread;
                thread.Start(_events[i]);
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
                ThreadLoop((EventObj)obj);
            }
            catch (Exception ex)
            {
                UnCatchedException.Invoke(ex);
            }
        }

        void ThreadLoop(EventObj obj)
        {
            bool flag = false;
            T t = default;
            for (; ; )
            {
                if (flag || queue.TryDequeue(out t))
                {
                    flag = false;
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
                    if (obj.IsDisposed) break;
                    if (queue.TryDequeue(out t)) { flag = true; continue; }
                    ResetThread(obj);
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
            if (Interlocked.CompareExchange(ref _set_sync, 1, 0) != 0) return;
            for (var i = 0; i < _limit; i++)
            {
                var y = _state;
                if (y == _allRun) return;
                var x = 1 << i;
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
                    Interlocked.Exchange(ref _set_sync, 0);
                    return;
                }
            }
            Interlocked.Exchange(ref _set_sync, 0);
        }

        private void ResetThread(EventObj ev)
        {
            var index = ev.Index;
            for (; ; )
            {
                var i = ~(1 << index);
                var x = _state;
                var j = x & i;
                var flag = Interlocked.CompareExchange(ref _state, j, x) == x;
                if (flag)
                {
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

        public void Dispose()
        {
            for (var i = 0; i < _limit; i++)
            {
                _events[i].IsDisposed = true;
                _events[i].Event.Set();
            }
        }
    }
}
