using System;
using System.Text;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class Contract: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(3);
             size +=stream.WriteHeader(EventPropertyType.L_32,1);
             size +=stream.WriteHeader(EventPropertyType.L_64,2);
             size +=stream.WriteHeader(EventPropertyType.L_Str,7);
             size +=stream.WriteIndex(1);
             size +=stream.WriteInt32((int)CommodityType);
             size +=stream.WriteIndex(4);
             size +=stream.WriteDouble(MarginValue);
             size +=stream.WriteIndex(5);
             size +=stream.WriteDouble(FreeValue);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(ExchangeNo);
             size +=stream.WriteIndex(2);
             size +=stream.WriteString(CommodityNo);
             size +=stream.WriteIndex(3);
             size +=stream.WriteString(InstrumentID);
             size +=stream.WriteIndex(6);
             size +=stream.WriteString(ContractExpDate);
             size +=stream.WriteIndex(7);
             size +=stream.WriteString(LastTradeDate);
             size +=stream.WriteIndex(8);
             size +=stream.WriteString(FirstNoticeDate);
             size +=stream.WriteIndex(9);
             size +=stream.WriteString(ContractName);
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
                    if (index == 1){ CommodityType = (CommodityType)stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 4){ MarginValue = stream.ReadDouble();continue;}
                    if (index == 5){ FreeValue = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ ExchangeNo = stream.ReadString();continue;}
                    if (index == 2){ CommodityNo = stream.ReadString();continue;}
                    if (index == 3){ InstrumentID = stream.ReadString();continue;}
                    if (index == 6){ ContractExpDate = stream.ReadString();continue;}
                    if (index == 7){ LastTradeDate = stream.ReadString();continue;}
                    if (index == 8){ FirstNoticeDate = stream.ReadString();continue;}
                    if (index == 9){ ContractName = stream.ReadString();continue;}
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
                var size=41+WriteStream.GetStringSize(ExchangeNo,encoding)+WriteStream.GetStringSize(CommodityNo,encoding)+WriteStream.GetStringSize(InstrumentID,encoding)+WriteStream.GetStringSize(ContractExpDate,encoding)+WriteStream.GetStringSize(LastTradeDate,encoding)+WriteStream.GetStringSize(FirstNoticeDate,encoding)+WriteStream.GetStringSize(ContractName,encoding)+ 0;
                return size;
        }
    }
}
