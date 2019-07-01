using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace YiDian.Soa.Sp
{
    public struct DataQueue<T>
    {
        public static DataQueue<T> Null;
        static int per_size = 0;
        static int Count = 0;
        static int TotalLength;
        static ConcurrentStack<int> Stack;
        static T[] orginal;
        static DataQueue()
        {
            Reload(100, 20);
        }
        public static void Reload(int persize, int count)
        {
            per_size = persize;
            Count = count;
            TotalLength = Count * per_size;
            orginal = new T[TotalLength];
            var list = new List<int>(Count);
            for (var i = 0; i < Count; i++)
            {
                list.Add(i * per_size);
            }
            Stack = new ConcurrentStack<int>(list);
        }
        public static bool Create(out DataQueue<T> flag)
        {
            flag = Null;
            var f = Stack.TryPop(out int i);
            if (!f) return f;
            flag = new DataQueue<T>(i, per_size);
            return true;
        }
        int index;
        readonly int offset;
        long flag;
        readonly int length;
        bool canWrite;
        private DataQueue(int off, int size)
        {
            offset = off;
            length = size;
            index = 0;
            flag = 0;
            canWrite = true;
        }
        public int Length { get { return index; } }
        public bool Enqueue(T t)
        {
            try
            {
                if (!canWrite) return false;
                Interlocked.Increment(ref flag);
                var x = Interlocked.Increment(ref index);
                if (x > length)
                {
                    canWrite = false;
                    Interlocked.Decrement(ref index);
                    Interlocked.Decrement(ref flag);
                    return false;
                }
                orginal[offset + x - 1] = t;
                Interlocked.Decrement(ref flag);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        void Stop()
        {
            if (!canWrite) return;
            canWrite = false;
            if (flag != 0)
            {
                var span = new SpinWait();
                while (Interlocked.Read(ref flag) != 0)
                {
                    span.SpinOnce();
                }
            }
        }
        public ReadOnlySpan<T> GetData()
        {
            Stop();
            if (index == 0) return null;
            return new ReadOnlySpan<T>(orginal, offset, index);
        }
        public void Reset()
        {
            Stack.Push(offset);
        }
    }
}
