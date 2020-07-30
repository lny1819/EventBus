using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// 
    /// </summary>
    public class ReadStream
    {
        readonly ReadOnlyMemory<byte> orginal;
        int Offset { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory"></param>
        public ReadStream(ReadOnlyMemory<byte> memory)
        {
            Offset = 0;
            orginal = memory;
            Encoding = Encoding.UTF8;
            StreamLength = memory.Length;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] GetOrginalDatas()
        {
            return orginal.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        public int DataSize
        {
            get { return BitConverter.ToInt32(orginal.Slice(0, 4).Span); }
        }
        /// <summary>
        /// 
        /// </summary>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int StreamLength { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int ReadInt32()
        {
            var i = BitConverter.ToInt32(orginal.Slice(Offset, 4).Span);
            Advance(4);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int ReReadInt32()
        {
            var i = BitConverter.ToInt32(orginal.Slice(Offset, 4).Span);
            Advance(4);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt32()
        {
            var i = BitConverter.ToUInt32(orginal.Slice(Offset, 4).Span);
            Advance(4);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public short ReadInt16()
        {
            var i = BitConverter.ToInt16(orginal.Slice(Offset, 2).Span);
            Advance(2);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ushort ReadUInt16()
        {
            var i = BitConverter.ToUInt16(orginal.Slice(Offset, 2).Span);
            Advance(2);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long ReadInt64()
        {
            var i = BitConverter.ToInt64(orginal.Slice(Offset, 8).Span);
            Advance(8);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong ReadUInt64()
        {
            var i = BitConverter.ToUInt64(orginal.Slice(Offset, 8).Span);
            Advance(8);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ReadDouble()
        {
            var i = BitConverter.ToDouble(orginal.Slice(Offset, 8).Span);
            Advance(8);
            return i;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string[] ReadArrayString()
        {
            var count = ReadInt32();
            var arrs = new string[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadString();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool[] ReadArrayBool()
        {
            var count = ReadInt32();
            var arrs = new bool[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadByte() == 1;
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DateTime[] ReadArrayDate()
        {
            var count = ReadInt32();
            var arrs = new DateTime[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadDate();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double[] ReadArrayDouble()
        {
            var count = ReadInt32();
            var arrs = new double[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadDouble();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int[] ReadArrayInt32()
        {
            var count = ReadInt32();
            var arrs = new int[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt32();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint[] ReadArrayUInt32()
        {
            var count = ReadInt32();
            var arrs = new uint[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt32();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long[] ReadArrayInt64()
        {
            var count = ReadInt32();
            var arrs = new long[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt64();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong[] ReadArrayUInt64()
        {
            var count = ReadInt32();
            var arrs = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt64();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public short[] ReadArrayInt16()
        {
            var count = ReadInt32();
            var arrs = new short[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadInt16();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ushort[] ReadArrayUInt16()
        {
            var count = ReadInt32();
            var arrs = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadUInt16();
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<byte> ReadArrayByte()
        {
            var count = ReadInt32();
            var span = orginal.Slice(Offset, count).Span;
            Advance(count);
            return span;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ReadArray(Type type)
        {
            var count = ReadInt32();
            var arrs = (IList<object>)Activator.CreateInstance(type.MakeArrayType(), new object[] { count });
            for (var i = 0; i < count; i++)
            {
                arrs[i] = ReadEventObj(type);
            }
            return arrs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ReadEventObj(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            var obj = constructor.Invoke(null) as IYiDianSeralize;
            var read = new ReadStream(orginal.Slice(Offset));
            obj.BytesTo(read);
            Offset += read.DataSize;
            return obj;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            var count = ReadInt32();
            if (count == 0) return string.Empty;
            var value = Encoding.GetString(orginal.Slice(Offset, count).Span);
            Advance(count);
            return value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            var b = orginal.Slice(Offset, 1).Span[0];
            Advance(1);
            return b;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        public void Advance(int length)
        {
            Offset += length;
        }
    }
}
