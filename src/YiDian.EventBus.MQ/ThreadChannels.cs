using System;
using System.Collections.Concurrent;
using System.Threading;


namespace YiDian.EventBus.MQ
{
    public class ThreadChannels
    {
        static readonly object lockobj = new object();
        static ThreadChannels _current;
        public static ThreadChannels Default
        {
            get
            {
                if (_current == null)
                {
                    lock (lockobj)
                    {
                        if (_current == null)
                            _current = new ThreadChannels(Math.Min(16, Environment.ProcessorCount));
                    }
                }
                return _current;
            }
        }
        public static Action<Exception> UnCatchedException;
        readonly Thread[] _threads;
        ConcurrentQueue<__ITEM> queue;
        int _limit;
        int _state = 0;
        readonly int _allRun = 0;
        public static ThreadChannels Create(int limit)
        {
            return new ThreadChannels(limit, true);
        }
        internal ThreadChannels(int limit, bool highlvl = false)
        {
            _limit = limit;
            _threads = new Thread[_limit];
            queue = new ConcurrentQueue<__ITEM>();
            ExecutionContext.SuppressFlow();
            for (var i = 0; i < limit; i++)
            {
                _allRun |= (1 << i);
            }
            for (var i = 0; i < limit; i++)
            {
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
                var flag = queue.TryDequeue(out __ITEM action);
                if (flag)
                {
                    try
                    {
                        action.Action.Invoke(action.ActionObj);
                    }
                    catch (Exception ex)
                    {
                        UnCatchedException?.Invoke(ex);
                    }
                }
                else
                {
                    lock (thread)
                    {
                        SetThread(index);
                        Monitor.Wait(thread, Timeout.Infinite);
                    }
                }
            }
        }

        struct __ITEM
        {
            public __ITEM(Action<object> action, object indata)
            {
                Action = action;
                ActionObj = indata;
            }
            public Action<object> Action { get; }
            public object ActionObj { get; }
        }
        public void QueueWorkItemInternal(Action<object> action, object indata = null)
        {
            queue.Enqueue(new __ITEM(action, indata));
            var thread = GetSetThread();
            if (thread == null) return;
            lock (thread)
            {
                Monitor.PulseAll(thread);
            }
        }

        private Thread GetSetThread()
        {
            if (_state == _allRun) return null;
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                return _threads[0];
            }
            for (var i = 0; i < _limit; i++)
            {
                if (_state == _allRun) return null;
                var x = 1 << i;
                var y = _state;
                var r = y | x;
                if (r != y)
                {
                    var flag = Interlocked.CompareExchange(ref _state, r, y) == y;
                    if (flag) return _threads[i];
                }
            }
            return null;
        }

        private void SetThread(int index)
        {
            for (; ; )
            {
                var i = ~(1 << index);
                var x = _state;
                var j = x & i;
                var flag = Interlocked.CompareExchange(ref _state, j, x) == x;
                if (flag) break;
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
