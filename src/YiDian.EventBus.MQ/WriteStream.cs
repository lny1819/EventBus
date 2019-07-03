using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class WriteStream
    {
        readonly byte[] orginal;
        int offset = 0;
        public WriteStream(uint size)
        {
            orginal = new byte[size];
        }
        Span<byte> Advance(int length)
        {
            var span = new Span<byte>(orginal, offset, length);
            offset += length;
            return span;
        }
        public void WriteHeader(EventPropertyType type, byte length)
        {
            var span = Advance(2);
            span[0] = (byte)type;
            span[1] = length;
        }
        unsafe public uint WriteString(string value)
        {
            var l = Encoding.UTF8.GetByteCount(value);
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, l);
            span = Advance(l);
            fixed (char* cPtr = value)
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    Encoding.UTF8.GetBytes(cPtr, value.Length, bPtr, l);
                }
            }
            return (uint)l;
        }
        public void WriteIndex(byte index)
        {
            WriteByte(index);
        }
        public void WriteByte(byte value)
        {
            var span = Advance(1);
            span[0] = value;
        }
        public void WriteInt16(short value)
        {
            var span = Advance(2);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteUInt16(ushort value)
        {
            var span = Advance(2);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteInt32(int value)
        {
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteUInt32(uint value)
        {
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteInt64(long value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteUInt64(ulong value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
        }
        public void WriteDouble(double value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
        }
        public unsafe uint WriteDate(DateTime value)
        {
            var v = value.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var span = Advance(23);
            fixed (char* cPtr = v)
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    Encoding.UTF8.GetBytes(cPtr, 23, bPtr, 23);
                }
            }
            return 23;
        }
        public void WriteArrayByte(IEnumerable<byte> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteByte(ider.Current);
                size += 1;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayString(IEnumerable<string> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            uint size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteString(ider.Current);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayDate(IEnumerable<DateTime> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            uint size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteDate(ider.Current);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayBool(IEnumerable<bool> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            uint size = count;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteByte(ider.Current ? (byte)1 : (byte)0);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayInt16(IEnumerable<short> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteInt16(ider.Current);
                size += 2;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayUInt16(IEnumerable<ushort> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteUInt16(ider.Current);
                size += 2;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayInt32(IEnumerable<int> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteInt32(ider.Current);
                size += 4;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayUInt32(IEnumerable<uint> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteUInt32(ider.Current);
                size += 4;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayInt64(IEnumerable<long> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteInt64(ider.Current);
                size += 8;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayUInt64(IEnumerable<ulong> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteUInt64(ider.Current);
                size += 8;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayDouble(IEnumerable<double> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteDouble(ider.Current);
                size += 8;
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteEventArray<T>(IEnumerable<T> value) where T : IYiDianSeralize
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return;
            }
            var span = Advance(4);
            uint size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteEventObj(ider.Current);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public uint WriteEventObj(IYiDianSeralize obj)
        {
            return obj.ToBytes(this);
        }
        public byte[] GetBytes()
        {
            var res = new byte[offset];
            Array.Copy(orginal, res, offset);
            return res;
        }
        public static uint GetStringSize(string value)
        {
            var l = (uint)Encoding.UTF8.GetByteCount(value);
            return l + 4;
        }
        public static uint GetArrayStringSize(IEnumerable<string> arr)
        {
            var count = arr == null ? 0 : (uint)arr.Count();
            if (count == 0) return 8;
            uint size = 0;
            var ider = arr.GetEnumerator();
            while (ider.MoveNext())
            {
                size += GetStringSize(ider.Current);
            }
            return size + 8;
        }
        public static uint GetValueArraySize<T>(byte perszie, IEnumerable<T> arr)
        {
            var count = arr == null ? 0 : (uint)arr.Count();
            return perszie * count + 8;
        }
        public static uint GetArrayEventObjSize<T>(IEnumerable<T> arr) where T : IYiDianSeralize
        {
            var count = arr == null ? 0 : (uint)arr.Count();
            if (count == 0) return 8;
            uint size = 0;
            var ider = arr.GetEnumerator();
            while (ider.MoveNext())
            {
                size += ider.Current.Size();
            }
            return size + 8;
        }
    }
}
