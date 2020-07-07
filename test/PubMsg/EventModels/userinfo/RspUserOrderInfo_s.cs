using System;
using System.Text;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RspUserOrderInfo: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(3);
             size +=stream.WriteHeader(EventPropertyType.L_32,4);
             size +=stream.WriteHeader(EventPropertyType.L_64,1);
             size +=stream.WriteHeader(EventPropertyType.L_Str,5);
             size +=stream.WriteIndex(5);
             size +=stream.WriteUInt32(Size);
             size +=stream.WriteIndex(7);
             size +=stream.WriteInt32((int)State);
             size +=stream.WriteIndex(8);
             size +=stream.WriteUInt32(FillSize);
             size +=stream.WriteIndex(9);
             size +=stream.WriteInt32((int)Action);
             size +=stream.WriteIndex(6);
             size +=stream.WriteDouble(Price);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(LocalOrderNo);
             size +=stream.WriteIndex(1);
             size +=stream.WriteString(Exchange);
             size +=stream.WriteIndex(2);
             size +=stream.WriteString(ContractId);
             size +=stream.WriteIndex(3);
             size +=stream.WriteString(Commodity);
             size +=stream.WriteIndex(4);
             size +=stream.WriteString(ServiceNo);
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
                    if (index == 5){ Size = stream.ReadUInt32();continue;}
                    if (index == 7){ State = (OrderState)stream.ReadInt32();continue;}
                    if (index == 8){ FillSize = stream.ReadUInt32();continue;}
                    if (index == 9){ Action = (OrderActType)stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 6){ Price = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ LocalOrderNo = stream.ReadString();continue;}
                    if (index == 1){ Exchange = stream.ReadString();continue;}
                    if (index == 2){ ContractId = stream.ReadString();continue;}
                    if (index == 3){ Commodity = stream.ReadString();continue;}
                    if (index == 4){ ServiceNo = stream.ReadString();continue;}
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
                var size=45+WriteStream.GetStringSize(LocalOrderNo,encoding)+WriteStream.GetStringSize(Exchange,encoding)+WriteStream.GetStringSize(ContractId,encoding)+WriteStream.GetStringSize(Commodity,encoding)+WriteStream.GetStringSize(ServiceNo,encoding)+ 0;
                return size;
        }
    }
}