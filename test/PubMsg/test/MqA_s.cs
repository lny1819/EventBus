using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.MyTest
{
    public partial class MqA: IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            var size = Size();
            stream.WriteUInt32(size);
            stream.WriteByte(6);
            stream.WriteHeader(EventPropertyType.L_8,1);
            stream.WriteHeader(EventPropertyType.L_32,2);
            stream.WriteHeader(EventPropertyType.L_64,1);
            stream.WriteHeader(EventPropertyType.L_Str,3);
            stream.WriteHeader(EventPropertyType.L_Array,4);
            stream.WriteHeader(EventPropertyType.L_N,1);
            stream.WriteIndex(6);
            stream.WriteByte(Flag ? (byte)1 : (byte)0);
            stream.WriteIndex(5);
            stream.WriteInt32(Type);
            stream.WriteIndex(9);
            stream.WriteInt32(Index);
            stream.WriteIndex(10);
            stream.WriteDouble(Amount);
            stream.WriteIndex(0);
            stream.WriteString(PropertyA);
            stream.WriteIndex(1);
            stream.WriteString(PropertyB);
            stream.WriteIndex(7);
            stream.WriteDate(Date);
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
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 6){ Flag = stream.ReadByte() == 1;continue;}
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
                    if (index == 5){ Type = stream.ReadUInt32();continue;}
                    if (index == 9){ Index = stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 10){ Amount = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ PropertyA = stream.ReadString();continue;}
                    if (index == 1){ PropertyB = stream.ReadString();continue;}
                    if (index == 7){ Date = stream.ReadDate();continue;}
                     var c = stream.ReadInt32();stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 3){ PropertyLC = stream.ReadArrayString();continue;}
                    if (index == 4){ PropertyD = stream.ReadArrayString();continue;}
                    if (index == 8){ QBS = stream.ReadArray<MqB>();continue;}
                    if (index == 11){ Amounts = stream.ReadArrayDouble();continue;}
                    var c = stream.ReadInt32();stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_N, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 2){ PropertyQB = new MqB(); PropertyQB.BytesTo(stream);continue;}
                    var l = stream.ReadInt32();
                    stream.Advance(l);
                }
            }
        }
        public uint Size()
        {
                var size=46+WriteStream.GetStringSize(PropertyA)+WriteStream.GetStringSize(PropertyB)+23+WriteStream.GetArrayStringSize(PropertyLC)+WriteStream.GetArrayStringSize(PropertyD)+WriteStream.GetArrayEventObjSize(QBS)+WriteStream.GetValueArraySize(8,Amounts)+PropertyQB.Size()+ 0;
                return size;
        }
    }
}
