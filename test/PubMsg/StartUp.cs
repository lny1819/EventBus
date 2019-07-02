using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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

            MqA xa = new MqA() { A = "a", B = "b", LC = new List<string>() { "22", "hello" }, Type = MqType.LS, D = new string[2] { "a", "b" }, QB = new MqB() { D = new string[] { "e", "f" }, C = "zs" } };
            var stream = new WriteStream();
            xa.ToBytes(stream);

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
        public void ToBytes(WriteStream stream)
        {
            //int32 Int64 UInt32 UInt64 double date bool string other
            stream.WriteByte(4);
            stream.WriteHeader(EventPropertyType.L_32, 1);
            stream.WriteHeader(EventPropertyType.L_Str, 2);
            stream.WriteHeader(EventPropertyType.L_Array, 2);
            stream.WriteHeader(EventPropertyType.L_N, 1);
            stream.WriteInt32((int)Type);
            stream.WriteString(A);
            stream.WriteString(B);
            stream.WriteArrayString(D);
            stream.WriteArrayString(LC);
            stream.WriteEventObj(QB);
        }
        public MqA BytesTo(ReadStream datas)
        {

        }
    }
    public enum MqType : byte
    {
        ZS = 1,
        LS = 2
    }
    public class MqB : IMQSeralize
    {
        [SeralizeIndex(0)]
        public string C { get; set; }
        [SeralizeIndex(1)]
        public string[] D { get; set; }

        public void ToBytes(WriteStream stream)
        {
            stream.WriteByte(2);
            stream.WriteHeader(EventPropertyType.L_Str, 1);
            stream.WriteHeader(EventPropertyType.L_Array, 1);
            stream.WriteString(C);
            stream.WriteArrayString(D);
        }
        public MqB BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();

        }
    }
    public interface IMQSeralize
    {
        void ToBytes(WriteStream stream);
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
        unsafe public void WriteString(string value)
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
        }
        public void WriteByte(byte value)
        {
            var span = Advance(1);
            span[0] = value;
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
        public void WriteArrayString(IEnumerable<string> value)
        {
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteString(ider.Current);
            }
        }
        public void WriteArrayInt32(IEnumerable<int> value)
        {
            var ider = value.GetEnumerator();
            while (ider.MoveNext())
            {
                WriteInt32(ider.Current);
            }
        }
        public void WriteEventObj(IMQSeralize obj)
        {
            obj.ToBytes(this);
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
        public Header[] ReadHeaders()
        {
            byte count = ReadByte();
            var headers = new Header[count];
            for (var i = 0; i < count; i++)
            {
                var type = (EventPropertyType)ReadByte();
                var c = ReadByte();
                var h = new Header() { Count = c, Type = type };
                headers[i] = h;
            }
            return headers;
        }
        public byte ReadByte()
        {
            var b = orginal[offset];
            offset += 1;
            return b;
        }
    }
    public struct Header
    {
        public EventPropertyType Type { get; set; }
        public byte Count { get; set; }
    }
    public enum EventPropertyType : byte
    {
        L_32,
        L_64,
        L_Str,
        L_Date,
        L_Array,
        L_N
    }
}
