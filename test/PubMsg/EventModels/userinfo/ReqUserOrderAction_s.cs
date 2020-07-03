using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class ReqUserOrderAction: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(3);
             size +=stream.WriteHeader(EventPropertyType.L_32,2);
             size +=stream.WriteHeader(EventPropertyType.L_64,3);
             size +=stream.WriteHeader(EventPropertyType.L_Str,1);
             size +=stream.WriteIndex(4);
             size +=stream.WriteUInt32(CommitSize);
             size +=stream.WriteIndex(5);
             size +=stream.WriteInt32((int)Action);
             size +=stream.WriteIndex(1);
             size +=stream.WriteDouble(StopProfit);
             size +=stream.WriteIndex(2);
             size +=stream.WriteDouble(StopLoss);
             size +=stream.WriteIndex(3);
             size +=stream.WriteDouble(CommitPrice);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(LocalOrderNo);
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
                    if (index == 4){ CommitSize = stream.ReadUInt32();continue;}
                    if (index == 5){ Action = (UserAction)stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 1){ StopProfit = stream.ReadDouble();continue;}
                    if (index == 2){ StopLoss = stream.ReadDouble();continue;}
                    if (index == 3){ CommitPrice = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ LocalOrderNo = stream.ReadString();continue;}
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
                var size=49+WriteStream.GetStringSize(LocalOrderNo)+ 0;
                return size;
        }
    }
}
