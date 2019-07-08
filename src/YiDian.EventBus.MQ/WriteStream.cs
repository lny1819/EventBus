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
        public Span<byte> Advance(int length)
        {
            var span = new Span<byte>(orginal, offset, length);
            offset += length;
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
            if (value == string.Empty)
            {
                var span2 = Advance(4);
                BitConverter.TryWriteBytes(span2, 0);
                return 4;
            }
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
        public uint WriteArrayByte(IEnumerable<byte> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
            }
            var span = Advance(4);
            uint size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteByte(ider.Current);
                size += 1;
            }
            BitConverter.TryWriteBytes(span, size);
            return size + 8;
        }
        public uint WriteArrayString(IEnumerable<string> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + size;
        }
        public uint WriteArrayDate(IEnumerable<DateTime> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + size;
        }
        public uint WriteArrayBool(IEnumerable<bool> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + size;
        }
        public uint WriteArrayInt16(IEnumerable<short> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayUInt16(IEnumerable<ushort> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayInt32(IEnumerable<int> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayUInt32(IEnumerable<uint> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayInt64(IEnumerable<long> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayUInt64(IEnumerable<ulong> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteArrayDouble(IEnumerable<double> value)
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + (uint)size;
        }
        public uint WriteEventArray<T>(IEnumerable<T> value) where T : IYiDianSeralize
        {
            var count = value == null ? 0 : (uint)value.Count();
            if (count == 0)
            {
                WriteInt32(0);
                WriteUInt32(0);
                return 8;
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
            return 8 + size;
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
