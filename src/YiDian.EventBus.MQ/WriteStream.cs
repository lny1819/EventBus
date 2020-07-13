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
        int start;
        public WriteStream(uint size)
        {
            start = Length = 0;
            orginal = new byte[size];
            Encoding = Encoding.UTF8;
        }
        public int Length { get; private set; }
        public WriteStream(byte[] bs, int index)
        {
            start = index;
            Length = 0;
            orginal = bs;
            Encoding = Encoding.UTF8;
        }
        public Encoding Encoding { get; set; }
        public Span<byte> Advance(int length)
        {
            var span = new Span<byte>(orginal, start + Length, length);
            Length += length;
            return span;
        }
        public uint WriteHeader(EventPropertyType type, byte length)
        {
            var span = Advance(2);
            span[0] = (byte)type;
            span[1] = length;
            return 2;
        }
        unsafe public uint WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                var span2 = Advance(4);
                BitConverter.TryWriteBytes(span2, 0);
                return 4;
            }
            var l = Encoding.GetByteCount(value);
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, l);
            span = Advance(l);
            fixed (char* cPtr = value)
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    var a = Encoding.GetBytes(cPtr, value.Length, bPtr, l);
                }
            }
            return (uint)l + 4;
        }
        public uint WriteIndex(byte index)
        {
            return WriteByte(index);
        }
        public uint WriteByte(byte value)
        {
            var span = Advance(1);
            span[0] = value;
            return 1;
        }
        public uint WriteInt16(short value)
        {
            var span = Advance(2);
            BitConverter.TryWriteBytes(span, value);
            return 2;
        }
        public uint WriteUInt16(ushort value)
        {
            var span = Advance(2);
            BitConverter.TryWriteBytes(span, value);
            return 2;
        }
        public uint WriteInt32(int value)
        {
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, value);
            return 4;
        }
        public uint WriteUInt32(uint value)
        {
            var span = Advance(4);
            BitConverter.TryWriteBytes(span, value);
            return 4;
        }
        public uint WriteInt64(long value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
            return 8;
        }
        public uint WriteUInt64(ulong value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
            return 8;
        }
        public uint WriteDouble(double value)
        {
            var span = Advance(8);
            BitConverter.TryWriteBytes(span, value);
            return 8;
        }
        public unsafe uint WriteDate(DateTime value)
        {
            var span = Advance(2);
            BitConverter.TryWriteBytes(span, (ushort)value.Year);
            span = Advance(2);
            BitConverter.TryWriteBytes(span, (ushort)value.Month);
            span = Advance(2);
            BitConverter.TryWriteBytes(span, (ushort)value.Day);
            span = Advance(3);
            span[0] = (byte)value.Hour;
            span[1] = (byte)value.Minute;
            span[2] = (byte)value.Second;
            span = Advance(2);
            var f = BitConverter.TryWriteBytes(span, (ushort)value.Millisecond);
            return 11;
        }
        public uint WriteArrayByte(byte[] value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            Buffer.BlockCopy(value, 0, orginal, Length + start, count);
            Length += count;
            return 4 + (uint)count;
        }
        public uint WriteArrayString(IEnumerable<string> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteString(ider.Current);
            }
            return size;
        }
        public uint WriteArrayDate(IEnumerable<DateTime> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteDate(ider.Current);
            }
            return size;
        }
        public uint WriteArrayBool(IEnumerable<bool> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteByte(ider.Current ? (byte)1 : (byte)0);
            }
            return size;
        }
        public uint WriteArrayInt16(IEnumerable<short> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteInt16(ider.Current);
            }
            return size;
        }
        public uint WriteArrayUInt16(IEnumerable<ushort> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteUInt16(ider.Current);
            }
            return size;
        }
        public uint WriteArrayInt32(IEnumerable<int> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteInt32(ider.Current);
            }
            return size;
        }
        public uint WriteArrayUInt32(IEnumerable<uint> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteUInt32(ider.Current);
            }
            return size;
        }
        public uint WriteArrayInt64(IEnumerable<long> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteInt64(ider.Current);
            }
            return size;
        }
        public uint WriteArrayUInt64(IEnumerable<ulong> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return 8;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteUInt64(ider.Current);
            }
            return size;
        }
        public uint WriteArrayDouble(IEnumerable<double> value)
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteDouble(ider.Current);
            }
            return size;
        }
        public uint WriteEventArray<T>(IEnumerable<T> value) where T : IYiDianSeralize
        {
            uint size = 4;
            var count = value == null ? 0 : value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                return size;
            }
            WriteInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                var size1 = WriteEventObj(ider.Current);
                size += size1;
            }
            return size;
        }
        public uint WriteEventObj(IYiDianSeralize obj)
        {
            return obj.ToBytes(this);
        }
        public ReadOnlyMemory<byte> GetBytes()
        {
            return new ReadOnlyMemory<byte>(orginal, start, Length);
        }
        public static uint GetStringSize(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value)) return 4;
            var l = (uint)encoding.GetByteCount(value);
            return l + 4;
        }
        public static uint GetArrayStringSize(IEnumerable<string> arr, Encoding encoding)
        {
            uint size = 4;
            var count = arr == null ? 0 : (uint)arr.Count();
            if (count == 0) return size;
            var ider = arr.GetEnumerator();
            while (ider.MoveNext())
            {
                size += GetStringSize(ider.Current, encoding);
            }
            return size;
        }
        public static uint GetValueArraySize<T>(byte perszie, IEnumerable<T> arr)
        {
            var count = arr == null ? 0 : (uint)arr.Count();
            return perszie * count + 4;
        }
        public static uint GetArrayEventObjSize<T>(IEnumerable<T> arr, Encoding encoding) where T : IYiDianSeralize
        {
            uint size = 4;
            var count = arr == null ? 0 : (uint)arr.Count();
            if (count == 0) return size;
            var ider = arr.GetEnumerator();
            while (ider.MoveNext())
            {
                size += ider.Current.BytesSize(encoding);
            }
            return size;
        }
    }
}
