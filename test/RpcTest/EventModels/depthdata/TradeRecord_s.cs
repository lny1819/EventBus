using System;
using System.Text;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.depthdata
{
    public partial class TradeRecord: IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(4);
             size +=stream.WriteHeader(EventPropertyType.L_Date,1);
             size +=stream.WriteHeader(EventPropertyType.L_32,1);
             size +=stream.WriteHeader(EventPropertyType.L_64,3);
             size +=stream.WriteHeader(EventPropertyType.L_Str,5);
             size +=stream.WriteIndex(7);
             size +=stream.WriteDate(TradeTime);
             size +=stream.WriteIndex(9);
             size +=stream.WriteInt32(ZCVolume);
             size +=stream.WriteIndex(3);
             size +=stream.WriteDouble(LastPrice);
             size +=stream.WriteIndex(6);
             size +=stream.WriteUInt64(TotalVolume);
             size +=stream.WriteIndex(8);
             size +=stream.WriteUInt64(Volume);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(CommodityNo);
             size +=stream.WriteIndex(1);
             size +=stream.WriteString(ExchangeID);
             size +=stream.WriteIndex(2);
             size +=stream.WriteString(InstrumentID);
             size +=stream.WriteIndex(4);
             size +=stream.WriteString(Oper);
             size +=stream.WriteIndex(5);
             size +=stream.WriteString(InTime);
            BitConverter.TryWriteBytes(span, size);
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
            if (headers.TryGetValue(EventPropertyType.L_Date, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 7){ TradeTime = stream.ReadDate();continue;}
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
                    if (index == 9){ ZCVolume = stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 3){ LastPrice = stream.ReadDouble();continue;}
                    if (index == 6){ TotalVolume = stream.ReadUInt64();continue;}
                    if (index == 8){ Volume = stream.ReadUInt64();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ CommodityNo = stream.ReadString();continue;}
                    if (index == 1){ ExchangeID = stream.ReadString();continue;}
                    if (index == 2){ InstrumentID = stream.ReadString();continue;}
                    if (index == 4){ Oper = stream.ReadString();continue;}
                    if (index == 5){ InTime = stream.ReadString();continue;}
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
        public uint BytesSize(Encoding encoding)
        {
                var size=62+WriteStream.GetStringSize(CommodityNo,encoding)+WriteStream.GetStringSize(ExchangeID,encoding)+WriteStream.GetStringSize(InstrumentID,encoding)+WriteStream.GetStringSize(Oper,encoding)+WriteStream.GetStringSize(InTime,encoding)+ 0;
                return size;
        }
    }
}
