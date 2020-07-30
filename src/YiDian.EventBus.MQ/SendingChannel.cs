using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YiDian.EventBus.MQ
{
    internal class SendingChannel<T>
    {
        int count = 0;
        const int MaxLength = 100000;
        int state = 0;
        int start_count = 0;
        readonly ConcurrentQueue<T> source;
        Action<T> consumer;
        public SendingChannel(Action<T> action)
        {
            consumer = action;
            source = new ConcurrentQueue<T>();
        }
        public event EventHandler<Exception> OnError;
        public bool Enqueue(T item)
        {
            if (IsOnClosing()) return false;
            if (count >= MaxLength) return false;
            source.Enqueue(item);
            Interlocked.Increment(ref count);
            StartConsumer();
            return true;
        }

        public bool Enqueue(IList<T> items)
        {
            if (items.Count > MaxLength) return false;
            if (IsOnClosing()) return false;
            if (count >= MaxLength) return false;
            foreach (var item in items) source.Enqueue(item);
            Interlocked.Add(ref count, items.Count);
            StartConsumer();
            return true;
        }
        private void StartConsumer()
        {
            if (!SetStart()) return;
            var _action = consumer;
            if (_action == null) return;
            Interlocked.Increment(ref start_count);
            Task.Run(() =>
            {
                for (; ; )
                {
                    var flag = source.TryDequeue(out T item);
                    if (!flag)
                    {
                        var spinWait = new SpinWait();
                        spinWait.SpinOnce();
                        while (!source.TryDequeue(out item))
                        {
                            if (Interlocked.CompareExchange(ref count, 0, 0) == 0)
                            {
                                SetStop();
                                break;
                            }
                            spinWait.SpinOnce();
                        }
                    }
                    Process(item);
                    var i = Interlocked.Decrement(ref count);
                    if (i <= 0)
                    {
                        SetStop();
                        break;
                    }
                }
            });
        }

        private void Process(T des)
        {
            if (consumer == null) return;
            try
            {
                consumer(des);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }


        public int Id { get; set; }
        internal void Clear()
        {
            consumer = null;
            if (state != 0)
            {
                var spinWait = new SpinWait();
                spinWait.SpinOnce();
                while (state != 0)
                {
                    spinWait.SpinOnce();
                }
            }
            count = 0;
        }
        public bool Using { get; internal set; }

        private bool IsOnClosing()
        {
            return state == 2;
        }
        private bool SetStart()
        {
            return Interlocked.CompareExchange(ref state, 1, 0) == 0;
        }

        private void SetStop()
        {
            Interlocked.CompareExchange(ref state, 0, 1);
        }
        public bool Close()
        {
            return Interlocked.Exchange(ref state, 2) != 2;
        }
        public int GetStartCountAndReset()
        {
            return Interlocked.Exchange(ref start_count, 0);
        }
    }
}
