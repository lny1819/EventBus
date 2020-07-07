using System;
using System.Text;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RspUseAction: IYiDianSeralize
    {
        public uint ToBytes(ref WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(4);
             size +=stream.WriteHeader(EventPropertyType.L_8,1);
             size +=stream.WriteHeader(EventPropertyType.L_32,2);
             size +=stream.WriteHeader(EventPropertyType.L_Str,1);
             size +=stream.WriteHeader(EventPropertyType.L_N,1);
             size +=stream.WriteIndex(1);
             size +=stream.WriteByte(IsLast ? (byte)1 : (byte)0);
             size +=stream.WriteIndex(0);
             size +=stream.WriteUInt32(SessionId);
             size +=stream.WriteIndex(2);
             size +=stream.WriteInt32((int)ErrorCode);
             size +=stream.WriteIndex(3);
             size +=stream.WriteString(ErrMsg);
             size +=stream.WriteIndex(4);
             size +=stream.WriteEventObj(Data);
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
                    if (index == 1){ IsLast = stream.ReadByte() == 1;continue;}
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
                    if (index == 0){ SessionId = stream.ReadUInt32();continue;}
                    if (index == 2){ ErrorCode = (ErrorCode)stream.ReadInt32();continue;}
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
                    if (index == 3){ ErrMsg = stream.ReadString();continue;}
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
                    if (index == 4){ Data = new RspUserOrderInfo(); Data.BytesTo(ref stream);continue;}
                    var l = stream.ReadInt32();
                    stream.Advance(l);
                }
            }
        }
        public uint BytesSize(Encoding encoding)
        {
                var size=27+WriteStream.GetStringSize(ErrMsg,encoding)+Data.BytesSize(encoding)+ 0;
                return size;
        }
    }
}
