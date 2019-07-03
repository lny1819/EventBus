using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.Seralize;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
            //soa.UseRabbitMq(Configuration["mqconnstr"], new JsonSeralizer())
            //     .UseDirectEventBus()
            //     .UseTopicEventBus();
        }
        public void Start(IServiceProvider sp, string[] args)
        {

            MqA xa = new MqA()
            {
                A = "a",
                B = "b",
                LC = new List<string>() { "22", "hello" },
                Type = MqType.LS,
                D = new string[2] { "a", "b" },
                QB = new MqB() { D = new string[] { "e1", "f1" }, C = "zs1" },
                Date = DateTime.Now,
                Flag = false,
                QBS = new List<MqB>()
                {
                       new MqB(){ D = new string[] { "e2", "f2" }, C = "zs2" },
                       new MqB(){ D = new string[] { "e3", "f3" }, C = "zs3" },
                       new MqB(){ D = new string[] { "e4", "f4" }, C = "zs4" }
                }
            };
            var stream = new WriteStream();
            xa.ToBytes(stream);
            var datas = stream.GetBytes();
            var reads = new ReadStream(datas);
            MqA xb = new MqA();
            xb.BytesTo(reads);

            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            res = eventsMgr.VaildityTest("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            var a = new MqA() { A = "a", B = "b2" };
            var b = new MqA() { A = "b", B = "b1" };
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            var ps = int.Parse(Configuration["ps"]);
            var type = Configuration["type"];
            var sleep = int.Parse(Configuration["sleep"]);
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = ps;
                    for (var j = 0; j < i; j++)
                    {
                        //topic.PublishPrefix(a, "s1");
                        //topic.Publish(a);
                        //direct.Publish(b);
                        //direct.Publish(a);
                        //direct.Publish(b);
                        qps.Add("p");
                        if (type == "direct")
                        {
                            direct.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-where")
                        {
                            topic.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-pre")
                        {
                            topic.PublishPrefix(a, "s1");
                            qps.Add("i");
                        }
                    }
                    Thread.Sleep(sleep);
                }
            });
        }
    }
    public class MqA : IMQEvent
    {
        [KeyIndex(0)]
        [SeralizeIndex(0)]
        public string A { get; set; }
        [SeralizeIndex(1)]
        public string B { get; set; }
        [SeralizeIndex(2)]
        public MqB QB { get; set; }
        [SeralizeIndex(3)]
        public List<string> LC { get; set; }
        [SeralizeIndex(4)]
        public string[] D { get; set; }
        [SeralizeIndex(5)]
        public MqType Type { get; set; }
        [SeralizeIndex(6)]
        public bool Flag { get; set; }
        [SeralizeIndex(7)]
        public DateTime Date { get; set; }
        [SeralizeIndex(8)]
        public List<MqB> QBS { get; set; }
        [SeralizeIndex(9)]
        public int Index { get; set; }
        [SeralizeIndex(10)]
        public double Amount { get; set; }
        [SeralizeIndex(11)]
        public double[] Amounts { get; set; }
        public int ToBytes(WriteStream stream)
        {
            stream.WriteIndex(6);
            stream.WriteByte(Flag ? (byte)1 : (byte)0);
            stream.WriteIndex(5);
            stream.WriteInt32((int)Type);
            stream.WriteIndex(9);
            stream.WriteInt32(Index);
            stream.WriteIndex(7);
            stream.WriteDate(Date);
            stream.WriteIndex(10);
            stream.WriteDouble(Amount);
            stream.WriteIndex(0);
            stream.WriteString(A);
            stream.WriteIndex(1);
            stream.WriteString(B);
            stream.WriteIndex(3);
            stream.WriteArrayString(LC, (uint)LC.Count);
            stream.WriteIndex(4);
            stream.WriteArrayString(D, (uint)D.Length);
            stream.WriteIndex(8);
            stream.WriteEventArray(QBS, (uint)QBS.Count);
            stream.WriteIndex(11);
            stream.WriteArrayDouble(Amounts, (uint)Amounts.Length);
            stream.WriteIndex(2);
            stream.WriteEventObj(QB);
            return 0;
        }
        public int Size()
        {
            var size = 1 + 6 * 2 + 1 + 4 * 2 + 8 * 2 + WriteStream.GetStringSize(A) + WriteStream.GetStringSize(B);

            return size;
        }
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))
            {
                var index = stream.ReadByte();
                if (index == 6) Flag = stream.ReadByte() == 1;
                for (var i = 0; i < count; i++)
                {
                    stream.Advance(2);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_32, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 5) Type = (MqType)stream.ReadInt32();
                    else if (index == 9) Index = stream.ReadInt32();
                    else stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 10) Amount = stream.ReadDouble();
                    if (index == 7) Date = stream.ReadInt64().UnixTimestampToDate();
                    else stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0) A = stream.ReadString();
                    else if (index == 1) B = stream.ReadString();
                    else
                    {
                        var c = stream.ReadInt32();
                        stream.Advance(c);
                    }
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 3) LC = stream.ReadArrayString().ToList();
                    else if (index == 4) D = stream.ReadArrayString();
                    else if (index == 8) QBS = stream.ReadArray<MqB>().ToList();
                    else
                    {
                        var l = stream.ReadUInt32();
                        stream.Advance((int)l);
                    }
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_N, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 2)
                    {
                        QB = new MqB();
                        QB.BytesTo(stream);
                    }
                }
            }
        }
    }
    public enum MqType : byte
    {
        ZS = 1,
        LS = 2
    }
    public class MqB : IYiDianSeralize
    {
        [SeralizeIndex(0)]
        public string C { get; set; }
        [SeralizeIndex(1)]
        public string[] D { get; set; }

        public int ToBytes(WriteStream stream)
        {
            stream.WriteByte(2);
            stream.WriteHeader(EventPropertyType.L_Str, 1);
            stream.WriteHeader(EventPropertyType.L_Array, 1);
            stream.WriteString(C);
            stream.WriteArrayString(D, (uint)D.Length);
            return 0;
        }
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_32, out byte count))
            {
                for (var i = 0; i < count; i++)
                {
                    stream.ReadInt32();
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    stream.ReadInt64();
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                C = stream.ReadString();
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                D = stream.ReadArrayString();
            }
        }
    }
    public static class TimeExtensions
    {
        static DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public static DateTime UnixTimestampToDate(this long timestamp)
        {
            return start.AddSeconds(timestamp).ToLocalTime();
        }
    }
    public interface IYiDianSeralize
    {
        int ToBytes(WriteStream stream);
        void BytesTo(ReadStream stream);
    }
    public class WriteStream
    {
        readonly byte[] orginal = new byte[2000];
        int offset = 0;
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
        public static int GetStringSize(string value)
        {
            var l = Encoding.UTF8.GetByteCount(value);
            return l + 4;
        }
        unsafe public int WriteString(string value)
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
            return l;
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
        public void WriteDate(DateTime value)
        {
            var v = value.ToUnixTimestamp();
            WriteInt64(v);
        }
        public void WriteArrayByte(IEnumerable<byte> value, uint count)
        {
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
        public void WriteArrayString(IEnumerable<string> value, uint count)
        {
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteString(ider.Current);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public void WriteArrayInt16(IEnumerable<short> value, uint count)
        {
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
        public void WriteArrayUInt16(IEnumerable<ushort> value, uint count)
        {
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
        public void WriteArrayInt32(IEnumerable<int> value, uint count)
        {
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
        public void WriteArrayUInt32(IEnumerable<uint> value, uint count)
        {
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
        public void WriteArrayInt64(IEnumerable<long> value, uint count)
        {
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
        public void WriteArrayUInt64(IEnumerable<ulong> value, uint count)
        {
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
        public void WriteArrayDouble(IEnumerable<double> value, uint count)
        {
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
        public void WriteEventArray<T>(IEnumerable<T> value, uint count) where T : IYiDianSeralize
        {
            var span = Advance(4);
            int size = 0;
            WriteUInt32(count);
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                size += WriteEventObj(ider.Current);
            }
            BitConverter.TryWriteBytes(span, size);
        }
        public int WriteEventObj(IYiDianSeralize obj)
        {
            return obj.ToBytes(this);
        }
        public byte[] GetBytes()
        {
            var res = new byte[offset];
            Array.Copy(orginal, res, offset);
            return res;
        }
    }
    public class ReadStream
    {
        readonly byte[] orginal;
        int offset = 0;
        public ReadStream(byte[] datas)
        {
            orginal = datas;
        }
        public Dictionary<EventPropertyType, byte> ReadHeaders()
        {
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
            t.BytesTo(this);
            return t;
        }
        public string ReadString()
        {
            var count = ReadInt32();
            var value = Encoding.UTF8.GetString(orginal, offset, count);
            offset += count;
            return value;
        }
        public DateTime ReadDate()
        {
            const int datecount = 23;
            var value = Encoding.UTF8.GetString(orginal, offset, datecount);
            offset += datecount;
            return DateTime.Parse(value);
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
    public struct Header
    {
        public EventPropertyType Type { get; set; }
        public byte Count { get; set; }
    }
    public enum EventPropertyType : byte
    {
        L_8,
        L_16,
        L_32,
        L_64,
        L_Str,
        L_Array,
        L_N
    }
}
