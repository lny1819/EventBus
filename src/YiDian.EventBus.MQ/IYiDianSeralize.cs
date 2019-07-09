using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public interface IYiDianSeralize
    {
        uint ToBytes(ref WriteStream stream);
        void BytesTo(ref ReadStream stream);
        uint Size();
    }
    public struct Header
    {
        public EventPropertyType Type { get; set; }
        public byte Count { get; set; }
    }
    public enum EventPropertyType : byte
    {
        L_8,
        L_16,
        L_32,
        L_Date,
        L_64,
        L_Str,
        L_Array,
        L_N
    }
}
