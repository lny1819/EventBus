using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.Seralize;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
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
                PropertyA = "hello mr li",
                PropertyB = "i am very happy",
                PropertyLC = new List<string>() { "2hellohello2hello", "hello very good" },
                Type = MqType.LS,
                PropertyD = new string[2] { "2hellohello2hello", "2hellohello2hello" },
                PropertyQB = new MqB() { D = new string[] { "2hellohello2hello", "2hellohello2hello" }, C = "zs1" },
                Date = DateTime.Now,
                Flag = false,
                QBS = new List<MqB>()
                {
                       new MqB(){ D = new string[] { "2hellohello2helloe2", "2hellohello2hello2" }, C = "zs2hellohello2hello2" },
                       new MqB(){ D = new string[] { "2hellohello2helloe3", "2hellohello2hellof3" }, C = "z2hellohello2hellos3" },
                       new MqB(){ D = new string[] { "2hellohello2helloe4", "2hellohello2hellof4" }, C = "z2hellohello2hellos4" }
                }
            };
            HttpEventsManager mgr = new HttpEventsManager("http://192.168.1.220:5000/api/event");
            mgr.RegisterEvent<MqA>("test", "1.4");
            //var json222 = xa.ToJson(); 
            //var l1 = Encoding.UTF8.GetBytes(json222).Length;
            //var l2 = xa.Size;
            //Console.ReadKey();
            //GC.Collect(2);

            //var count = 1000;
            //var watch = Stopwatch.StartNew();

            //for (var xx = 0; xx < count; xx++)
            //{
            //    var json = xa.ToJson();
            //    var bytes = Encoding.UTF8.GetBytes(json);
            //    var json2 = Encoding.UTF8.GetString(bytes);
            //    json2.JsonTo<MqA>();
            //}
            //watch.Stop();
            //Console.WriteLine("json test:" + watch.ElapsedMilliseconds.ToString());
            //GC.Collect(2);
            //Console.ReadKey();
            //watch.Restart();
            //for (var xx = 0; xx < count; xx++)
            //{
            //    var size = xa.Size;
            //    var stream = new WriteStream(size);
            //    xa.ToBytes(stream);
            //    var datas = stream.GetBytes();
            //    var reads = new ReadStream(datas);
            //    MqA xb = new MqA();
            //    xb.BytesTo(reads);
            //}
            //Console.WriteLine("mystream test:" + watch.ElapsedMilliseconds.ToString());
            //GC.Collect(2);
            //Console.ReadKey();

            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            res = eventsMgr.VaildityTest("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            var a = new MqA() { PropertyA = "a", PropertyB = "b2" };
            var b = new MqA() { PropertyA = "b", PropertyB = "b1" };
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
    public partial class MqA : IMQEvent, IYiDianSeralize
    {
        [KeyIndex(0)]
        [SeralizeIndex(0)]
        public string PropertyA { get; set; }
        [SeralizeIndex(1)]
        public string PropertyB { get; set; }
        [SeralizeIndex(2)]
        public MqB PropertyQB { get; set; }
        [SeralizeIndex(3)]
        public List<string> PropertyLC { get; set; }
        [SeralizeIndex(4)]
        public string[] PropertyD { get; set; }
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
        public uint ToBytes(WriteStream stream)
        {
            var size = Size();
            stream.WriteUInt32(size);
            stream.WriteByte(6);
            stream.WriteHeader(EventPropertyType.L_8, 1);
            stream.WriteHeader(EventPropertyType.L_32, 2);
            stream.WriteHeader(EventPropertyType.L_64, 2);
            stream.WriteHeader(EventPropertyType.L_Str, 2);
            stream.WriteHeader(EventPropertyType.L_Array, 4);
            stream.WriteHeader(EventPropertyType.L_N, 1);
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
            stream.WriteString(PropertyA);
            stream.WriteIndex(1);
            stream.WriteString(PropertyB);
            stream.WriteIndex(3);
            stream.WriteArrayString(PropertyLC);
            stream.WriteIndex(4);
            stream.WriteArrayString(PropertyD);
            stream.WriteIndex(8);
            stream.WriteEventArray(QBS);
            stream.WriteIndex(11);
            stream.WriteArrayDouble(Amounts);
            stream.WriteIndex(2);
            stream.WriteEventObj(PropertyQB);
            return size;
        }
        public uint Size()
        {
            var size = 5 + 6 * 2 + 12 + (1 * 1 + 4 * 2 + 8 * 2) + WriteStream.GetStringSize(PropertyA)
                     + WriteStream.GetStringSize(PropertyB) + WriteStream.GetArrayStringSize(PropertyLC) + WriteStream.GetArrayStringSize(PropertyD)
                     + WriteStream.GetArrayEventObjSize(QBS) + WriteStream.GetValueArraySize(8, Amounts)
                    + PropertyQB.Size();
            return size;
        }
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 6) Flag = stream.ReadByte() == 1;
                    else stream.Advance(1);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_16, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
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
                    else if (index == 7) Date = stream.ReadInt64().UnixTimestampToDate();
                    else stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0) PropertyA = stream.ReadString();
                    else if (index == 1) PropertyB = stream.ReadString();
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
                    if (index == 3) PropertyLC = stream.ReadArrayString().ToList();
                    else if (index == 4) PropertyD = stream.ReadArrayString();
                    else if (index == 8) QBS = stream.ReadArray<MqB>().ToList();
                    else if (index == 11) Amounts = stream.ReadArrayDouble();
                    else
                    {
                        var l = stream.ReadInt32();
                        stream.Advance(l);
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
                        PropertyQB = new MqB();
                        PropertyQB.BytesTo(stream);
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

        public uint ToBytes(WriteStream stream)
        {
            var size = Size();
            stream.WriteUInt32(size);
            stream.WriteByte(2);
            stream.WriteHeader(EventPropertyType.L_Str, 1);
            stream.WriteHeader(EventPropertyType.L_Array, 1);
            stream.WriteIndex(0);
            stream.WriteString(C);
            stream.WriteIndex(1);
            stream.WriteArrayString(D);
            return size;
        }
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(1);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_16, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(2);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_32, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0) C = stream.ReadString();
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
                    if (index == 1) D = stream.ReadArrayString();
                    else
                    {
                        var l = stream.ReadInt32();
                        stream.Advance(l);
                    }
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_N, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    var l = stream.ReadInt32();
                    stream.Advance(l);
                }
            }
        }
        public uint Size()
        {
            return 5 + 2 * 2 + 2 + WriteStream.GetStringSize(C) + WriteStream.GetArrayStringSize(D);
        }
    }
}
