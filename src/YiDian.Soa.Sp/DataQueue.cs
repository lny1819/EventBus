using System;
using System.Threading;

namespace YiDian.Soa.Sp
{
    public class DataQueue<T>
    {
        static int xx_flag = 0;
        int index;
        long flag;
        readonly int length;
        readonly T[] array;
        bool canWrite;
        public DataQueue(int l)
        {
            Id = Interlocked.Increment(ref xx_flag);
            length = l;
            index = 0;
            flag = 0;
            array = new T[length];
            canWrite = true;
        }
        public int Id { get; }
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
                array[x - 1] = t;
                Interlocked.Decrement(ref flag);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        public void Stop()
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
        public void Restart()
        {
            canWrite = true;
        }
        public T[] GetData()
        {
            Stop();
            if (index == 0) return null;
            if (index == length) return array;
            else
            {
                var des = new T[index];
                Array.Copy(array, des, index);
                return des;
            }
        }
    }
}
