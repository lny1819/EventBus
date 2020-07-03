using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RtnFillInfo: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(3);
             size +=stream.WriteHeader(EventPropertyType.L_32,1);
             size +=stream.WriteHeader(EventPropertyType.L_64,2);
             size +=stream.WriteHeader(EventPropertyType.L_Str,7);
             size +=stream.WriteIndex(3);
             size +=stream.WriteUInt32(FillSize);
             size +=stream.WriteIndex(4);
             size +=stream.WriteDouble(FillPrice);
             size +=stream.WriteIndex(6);
             size +=stream.WriteDouble(UpperFeeValue);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(LocalOrderNo);
             size +=stream.WriteIndex(1);
             size +=stream.WriteString(ServiceOrderNo);
             size +=stream.WriteIndex(2);
             size +=stream.WriteString(ServerMatchNo);
             size +=stream.WriteIndex(5);
             size +=stream.WriteString(TradeTime);
             size +=stream.WriteIndex(7);
             size +=stream.WriteString(Commodity);
             size +=stream.WriteIndex(8);
             size +=stream.WriteString(Contract);
             size +=stream.WriteIndex(9);
             size +=stream.WriteString(Exchange);
            BitConverter.TryWriteBytes(span, size);
            return size;
        }
        public void BytesTo(ref ReadStream stream)
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
            if (headers.TryGetValue(EventPropertyType.L_Date, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
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
                    if (index == 3){ FillSize = stream.ReadUInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 4){ FillPrice = stream.ReadDouble();continue;}
                    if (index == 6){ UpperFeeValue = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ LocalOrderNo = stream.ReadString();continue;}
                    if (index == 1){ ServiceOrderNo = stream.ReadString();continue;}
                    if (index == 2){ ServerMatchNo = stream.ReadString();continue;}
                    if (index == 5){ TradeTime = stream.ReadString();continue;}
                    if (index == 7){ Commodity = stream.ReadString();continue;}
                    if (index == 8){ Contract = stream.ReadString();continue;}
                    if (index == 9){ Exchange = stream.ReadString();continue;}
                     var c = stream.ReadInt32();stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    var c = stream.ReadInt32();stream.Advance(c);
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
        public uint BytesSize()
        {
                var size=41+WriteStream.GetStringSize(LocalOrderNo)+WriteStream.GetStringSize(ServiceOrderNo)+WriteStream.GetStringSize(ServerMatchNo)+WriteStream.GetStringSize(TradeTime)+WriteStream.GetStringSize(Commodity)+WriteStream.GetStringSize(Contract)+WriteStream.GetStringSize(Exchange)+ 0;
                return size;
        }
    }
}
