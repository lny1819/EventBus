﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public struct ReadStream
    {
        readonly byte[] orginal;
        int offset;
        public ReadStream(byte[] datas, int index = 0)
        {
            offset = index;
            orginal = datas;
            Encoding = Encoding.UTF8;
        }
        public Encoding Encoding { get; set; }
        public Dictionary<EventPropertyType, byte> ReadHeaders()
        {
            Advance(4);
            byte count = ReadByte();
            var headers = new Dictionary<EventPropertyType, byte>(count);
            for (var i = 0; i < count; i++)
            {
                var type = (EventPropertyType)ReadByte();
                var c = ReadByte();
                headers.Add(type, c);
            }
            return headers;
        }
        public int ReadInt32()
        {
            var i = BitConverter.ToInt32(orginal, offset);
            offset += 4;
            return i;
        }
        public uint ReadUInt32()
        {
            var i = BitConverter.ToUInt32(orginal, offset);
            offset += 4;
            return i;
        }
        public short ReadInt16()
        {
            var i = BitConverter.ToInt16(orginal, offset);
            offset += 2;
            return i;
        }
        public ushort ReadUInt16()
        {
            var i = BitConverter.ToUInt16(orginal, offset);
            offset += 2;
            return i;
        }
        public long ReadInt64()
        {
            var i = BitConverter.ToInt64(orginal, offset);
            offset += 8;
            return i;
        }
        public ulong ReadUInt64()
        {
            var i = BitConverter.ToUInt64(orginal, offset);
            offset += 8;
            return i;
        }
        public double ReadDouble()
        {
            var i = BitConverter.ToDouble(orginal, offset);
            offset += 8;
            return i;
        }
        public string[] ReadArrayString()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new string[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadString();
            }
            return arrs;
        }
        public bool[] ReadArrayBool()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new bool[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadByte() == 1;
            }
            return arrs;
        }
        public DateTime[] ReadArrayDate()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new DateTime[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadDate();
            }
            return arrs;
        }
        public double[] ReadArrayDouble()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new double[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadDouble();
            }
            return arrs;
        }
        public int[] ReadArrayInt32()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new int[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt32();
            }
            return arrs;
        }
        public uint[] ReadArrayUInt32()
        {
            Advance(4);
            var count = ReadUInt32();
            var arrs = new uint[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt32();
            }
            return arrs;
        }
        public long[] ReadArrayInt64()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new long[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt64();
            }
            return arrs;
        }
        public ulong[] ReadArrayUInt64()
        {
            Advance(4);
            var count = ReadUInt32();
            var arrs = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt64();
            }
            return arrs;
        }
        public short[] ReadArrayInt16()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new short[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt16();
            }
            return arrs;
        }
        public ushort[] ReadArrayUInt16()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt16();
            }
            return arrs;
        }
        public ReadOnlySpan<byte> ReadArrayByte()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new short[count];
            var span = new ReadOnlySpan<byte>(orginal, offset, count);
            offset += count;
            return span;
        }
        public T[] ReadArray<T>() where T : IYiDianSeralize, new()
        {
            Advance(4);
            var count = ReadInt32();
            var arrs = new T[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadEventObj<T>();
            }
            return arrs;
        }
        public T ReadEventObj<T>() where T : IYiDianSeralize, new()
        {
            var t = new T();
            t.BytesTo(ref this);
            return t;
        }
        public string ReadString()
        {
            var count = ReadInt32();
            if (count == 0) return string.Empty;
            var value = Encoding.GetString(orginal, offset, count);
            offset += count;
            return value;
        }
        public DateTime ReadDate()
        {
            var year = ReadUInt16();
            var month = ReadUInt16();
            var day = ReadUInt16();
            var hh = ReadByte();
            var mm = ReadByte();
            var ss = ReadByte();
            var sss = ReadUInt16();
            return new DateTime(year, month, day, hh, mm, ss, sss);
        }
        public byte ReadByte()
        {
            var b = orginal[offset];
            offset += 1;
            return b;
        }
        public void Advance(int length)
        {
            offset += length;
        }
    }
}
