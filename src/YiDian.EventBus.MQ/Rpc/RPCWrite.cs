using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    internal class RPCWrite
    {
        byte[] orginal;
        int offset = 0;
        Encoding encoding;
        public RPCWrite(byte[] datas, Encoding encode)
        {
            encoding = encode;
            orginal = datas;
        }
        public Span<byte> Advance(int length)
        {
            var span = new Span<byte>(orginal, offset, length);
            offset += length;
            return span;
        }
        unsafe public uint WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                orginal[offset] = (byte)'\r';
                offset += 1;
                return 1;
            }
            var l = encoding.GetByteCount(value);
            var span = Advance(l);
            fixed (char* cPtr = value)
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    encoding.GetBytes(cPtr, value.Length, bPtr, l);
                }
            }
            orginal[offset] = (byte)'\r';
            offset += 1;
            return (uint)l + 1;
        }
        unsafe public void WriteContent(ContentType type, object data, Type datatype)
        {
            orginal[offset] = (byte)'\r';
            offset += 1;
            var span = Advance(4);
            var length = 0;
            if (data != null)
            {
                var ser = CreateSeralize(type, encoding);
                length = ser.Serialize(data, datatype, orginal, offset);
                offset += length;
            }
            BitConverter.TryWriteBytes(span, length);
        }
        public ReadOnlyMemory<byte> GetDatas()
        {
            return new ReadOnlyMemory<byte>(orginal, 0, offset);
        }
        internal static IEventSeralize CreateSeralize(ContentType contentType, Encoding encoding)
        {
            switch (contentType)
            {
                case ContentType.Text:
                    return new TextSeralize(encoding);
                case ContentType.Json:
                    return new JsonSerializer(encoding);
                default:
                    return new DefaultYDSeralizer(encoding);
            }
        }
    }
}
