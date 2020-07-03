using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class UserInsertOrder: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(4);
             size +=stream.WriteHeader(EventPropertyType.L_8,2);
             size +=stream.WriteHeader(EventPropertyType.L_32,4);
             size +=stream.WriteHeader(EventPropertyType.L_64,3);
             size +=stream.WriteHeader(EventPropertyType.L_Str,6);
             size +=stream.WriteIndex(14);
             size +=stream.WriteByte(Locked ? (byte)1 : (byte)0);
             size +=stream.WriteIndex(15);
             size +=stream.WriteByte(IsCover ? (byte)1 : (byte)0);
             size +=stream.WriteIndex(3);
             size +=stream.WriteInt32((int)Side);
             size +=stream.WriteIndex(4);
             size +=stream.WriteInt32((int)TimeType);
             size +=stream.WriteIndex(5);
             size +=stream.WriteInt32((int)OrderType);
             size +=stream.WriteIndex(12);
             size +=stream.WriteUInt32(CommitSize);
             size +=stream.WriteIndex(1);
             size +=stream.WriteDouble(StopProfit);
             size +=stream.WriteIndex(2);
             size +=stream.WriteDouble(StopLoss);
             size +=stream.WriteIndex(11);
             size +=stream.WriteDouble(CommitPrice);
             size +=stream.WriteIndex(6);
             size +=stream.WriteString(Exchange);
             size +=stream.WriteIndex(7);
             size +=stream.WriteString(Commodity);
             size +=stream.WriteIndex(8);
             size +=stream.WriteString(Contract);
             size +=stream.WriteIndex(9);
             size +=stream.WriteString(UserInfo);
             size +=stream.WriteIndex(10);
             size +=stream.WriteString(Account);
             size +=stream.WriteIndex(13);
             size +=stream.WriteString(FromPositionId);
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
                    if (index == 14){ Locked = stream.ReadByte() == 1;continue;}
                    if (index == 15){ IsCover = stream.ReadByte() == 1;continue;}
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
                    if (index == 3){ Side = (OrderDirection)stream.ReadInt32();continue;}
                    if (index == 4){ TimeType = (OrderTimeType)stream.ReadInt32();continue;}
                    if (index == 5){ OrderType = (OrderType)stream.ReadInt32();continue;}
                    if (index == 12){ CommitSize = stream.ReadUInt32();continue;}
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
                    if (index == 11){ CommitPrice = stream.ReadDouble();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 6){ Exchange = stream.ReadString();continue;}
                    if (index == 7){ Commodity = stream.ReadString();continue;}
                    if (index == 8){ Contract = stream.ReadString();continue;}
                    if (index == 9){ UserInfo = stream.ReadString();continue;}
                    if (index == 10){ Account = stream.ReadString();continue;}
                    if (index == 13){ FromPositionId = stream.ReadString();continue;}
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
                var size=70+WriteStream.GetStringSize(Exchange)+WriteStream.GetStringSize(Commodity)+WriteStream.GetStringSize(Contract)+WriteStream.GetStringSize(UserInfo)+WriteStream.GetStringSize(Account)+WriteStream.GetStringSize(FromPositionId)+ 0;
                return size;
        }
    }
}
