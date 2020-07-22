using System;
using System.Text;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class ContractSubInfo: IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(2);
             size +=stream.WriteHeader(EventPropertyType.L_8,1);
             size +=stream.WriteHeader(EventPropertyType.L_Str,4);
             size +=stream.WriteIndex(4);
             size +=stream.WriteByte(Success ? (byte)1 : (byte)0);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(Exchange);
             size +=stream.WriteIndex(1);
             size +=stream.WriteString(Commodity);
             size +=stream.WriteIndex(2);
             size +=stream.WriteString(Contract);
             size +=stream.WriteIndex(3);
             size +=stream.WriteString(CoreID);
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
                    if (index == 4){ Success = stream.ReadByte() == 1;continue;}
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
                    if (index == 0){ Exchange = stream.ReadString();continue;}
                    if (index == 1){ Commodity = stream.ReadString();continue;}
                    if (index == 2){ Contract = stream.ReadString();continue;}
                    if (index == 3){ CoreID = stream.ReadString();continue;}
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
                var size=15+WriteStream.GetStringSize(Exchange,encoding)+WriteStream.GetStringSize(Commodity,encoding)+WriteStream.GetStringSize(Contract,encoding)+WriteStream.GetStringSize(CoreID,encoding)+ 0;
                return size;
        }
    }
}
