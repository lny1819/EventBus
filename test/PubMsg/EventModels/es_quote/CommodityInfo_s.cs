using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class CommodityInfo: IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(3);
             size +=stream.WriteHeader(EventPropertyType.L_32,5);
             size +=stream.WriteHeader(EventPropertyType.L_64,3);
             size +=stream.WriteHeader(EventPropertyType.L_Str,7);
             size +=stream.WriteIndex(2);
             size +=stream.WriteInt32((int)CommodityType);
             size +=stream.WriteIndex(10);
             size +=stream.WriteInt32(MarketDot);
             size +=stream.WriteIndex(11);
             size +=stream.WriteInt32(CommodityDenominator);
             size +=stream.WriteIndex(12);
             size +=stream.WriteInt32(DeliveryDays);
             size +=stream.WriteIndex(14);
             size +=stream.WriteInt32(CommodityTimeZone);
             size +=stream.WriteIndex(7);
             size +=stream.WriteDouble(ContractSize);
             size +=stream.WriteIndex(8);
             size +=stream.WriteDouble(StrikePriceTimes);
             size +=stream.WriteIndex(9);
             size +=stream.WriteDouble(CommodityTickSize);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(ExchangeNo);
             size +=stream.WriteIndex(1);
             size +=stream.WriteString(ExchangeName);
             size +=stream.WriteIndex(3);
             size +=stream.WriteString(CommodityNo);
             size +=stream.WriteIndex(4);
             size +=stream.WriteString(CommodityName);
             size +=stream.WriteIndex(5);
             size +=stream.WriteString(CommodityEngName);
             size +=stream.WriteIndex(6);
             size +=stream.WriteString(TradeCurrency);
             size +=stream.WriteIndex(13);
             size +=stream.WriteString(AddOneTime);
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
                    if (index == 2){ CommodityType = (CommodityType)stream.ReadInt32();continue;}
                    if (index == 10){ MarketDot = stream.ReadInt32();continue;}
                    if (index == 11){ CommodityDenominator = stream.ReadInt32();continue;}
                    if (index == 12){ DeliveryDays = stream.ReadInt32();continue;}
                    if (index == 14){ CommodityTimeZone = stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 7){ ContractSize = stream.ReadDouble();continue;}
                    if (index == 8){ StrikePriceTimes = stream.ReadDouble();continue;}
                    if (index == 9){ CommodityTickSize = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ ExchangeNo = stream.ReadString();continue;}
                    if (index == 1){ ExchangeName = stream.ReadString();continue;}
                    if (index == 3){ CommodityNo = stream.ReadString();continue;}
                    if (index == 4){ CommodityName = stream.ReadString();continue;}
                    if (index == 5){ CommodityEngName = stream.ReadString();continue;}
                    if (index == 6){ TradeCurrency = stream.ReadString();continue;}
                    if (index == 13){ AddOneTime = stream.ReadString();continue;}
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
        public uint Size()
        {
                var size=70+WriteStream.GetStringSize(ExchangeNo)+WriteStream.GetStringSize(ExchangeName)+WriteStream.GetStringSize(CommodityNo)+WriteStream.GetStringSize(CommodityName)+WriteStream.GetStringSize(CommodityEngName)+WriteStream.GetStringSize(TradeCurrency)+WriteStream.GetStringSize(AddOneTime)+ 0;
                return size;
        }
    }
}
