using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace YiDian.Soa.Sp
{
    public struct DataQueue<T>
    {
        static readonly int per_size = 2000;
        static readonly int Count = 20;
        static readonly int TotalLength = Count * per_size;
        static readonly Stack<int> Stack = new Stack<int>(Count);
        static readonly T[] orginal = new T[TotalLength];
        static DataQueue()
        {
            for (var i = 0; i < Count; i++)
            {
                Stack.Push(i * per_size);
            }
        }
        public static DataQueue<T> Create()
        {
            var i = Stack.Pop();
            return new DataQueue<T>(i);
        }
        int index;
        readonly int offset;
        long flag;
        readonly int length;
        bool canWrite;
        private DataQueue(int l)
        {
            offset = l;
            length = l;
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
