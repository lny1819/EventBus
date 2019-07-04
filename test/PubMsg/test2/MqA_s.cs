using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.MyTest
{
    public partial class MqA : IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(7);
            size += stream.WriteHeader(EventPropertyType.L_8, 1);
            size += stream.WriteHeader(EventPropertyType.L_Date, 1);
            size += stream.WriteHeader(EventPropertyType.L_32, 2);
            size += stream.WriteHeader(EventPropertyType.L_64, 1);
            size += stream.WriteHeader(EventPropertyType.L_Str, 2);
            size += stream.WriteHeader(EventPropertyType.L_Array, 4);
            size += stream.WriteHeader(EventPropertyType.L_N, 1);
            size += stream.WriteIndex(6);
            size += stream.WriteByte(Flag ? (byte)1 : (byte)0);
            size += stream.WriteIndex(7);
            size += stream.WriteDate(Date);
            size += stream.WriteIndex(5);
            size += stream.WriteInt32((int)Type);
            size += stream.WriteIndex(9);
            size += stream.WriteInt32(Index);
            size += stream.WriteIndex(10);
            size += stream.WriteDouble(Amount);
            size += stream.WriteIndex(0);
            size += stream.WriteString(PropertyA);
            size += stream.WriteIndex(1);
            size += stream.WriteString(PropertyB);
            size += stream.WriteIndex(3);
            size += stream.WriteArrayString(PropertyLC);
            size += stream.WriteIndex(4);
            size += stream.WriteArrayString(PropertyD);
            size += stream.WriteIndex(8);
            size += stream.WriteEventArray(QBS);
            size += stream.WriteIndex(11);
            size += stream.WriteArrayDouble(Amounts);
            size += stream.WriteIndex(2);
            size += stream.WriteEventObj(PropertyQB);
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
                    if (index == 6) { Flag = stream.ReadByte() == 1; continue; }
                    stream.Advance(1);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Date, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 7) { Date = stream.ReadDate(); continue; }
                    stream.Advance(11);
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
                    if (index == 5) { Type = (MqType)stream.ReadInt32(); continue; }
                    if (index == 9) { Index = stream.ReadInt32(); continue; }
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 10) { Amount = stream.ReadDouble(); continue; }
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0) { PropertyA = stream.ReadString(); continue; }
                    if (index == 1) { PropertyB = stream.ReadString(); continue; }
                    var c = stream.ReadInt32(); stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 3) { PropertyLC = stream.ReadArrayString(); continue; }
                    if (index == 4) { PropertyD = stream.ReadArrayString(); continue; }
                    if (index == 8) { QBS = stream.ReadArray<MqB>(); continue; }
                    if (index == 11) { Amounts = stream.ReadArrayDouble(); continue; }
                    var c = stream.ReadInt32(); stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_N, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 2) { PropertyQB = new MqB(); PropertyQB.BytesTo(stream); continue; }
                    var l = stream.ReadInt32();
                    stream.Advance(l);
                }
            }
        }
        public uint Size()
        {
            var size = 59 + WriteStream.GetStringSize(PropertyA) + WriteStream.GetStringSize(PropertyB) + WriteStream.GetArrayStringSize(PropertyLC) + WriteStream.GetArrayStringSize(PropertyD) + WriteStream.GetArrayEventObjSize(QBS) + WriteStream.GetValueArraySize(8, Amounts) + PropertyQB.Size() + 0;
            return size;
        }
    }
}
