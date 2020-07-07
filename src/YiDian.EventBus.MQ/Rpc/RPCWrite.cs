using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    internal class RPCWrite
    {
        byte[] orginal;
        int offset = 0;
        public RPCWrite(byte[] datas)
        {
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
            var l = Encoding.ASCII.GetByteCount(value);
            var span = Advance(l);
            fixed (char* cPtr = value)
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    Encoding.ASCII.GetBytes(cPtr, value.Length, bPtr, l);
                }
            }
            orginal[offset] = (byte)'\r';
            offset += 1;
            return (uint)l + 1;
        }
        unsafe public void WriteContent(ContentType type, object data, Type datatype, Encoding encoding)
        {
            orginal[offset] = (byte)'\r';
            offset += 1;
            var span = Advance(4);
            var ser = CreateSeralize(type, encoding);
            var length = ser.Serialize(data, datatype, orginal, offset);
            BitConverter.TryWriteBytes(span, length);
        }
        private IEventSeralize CreateSeralize(ContentType contentType, Encoding encoding)
        {
            switch (contentType)
            {
                case ContentType.Text:
                    return new TextSeralize(encoding);
                case ContentType.Json:
                    return new JsonSerializer(encoding);
                default:
                    return new DefaultSeralizer(encoding);
            }
        }
        public ReadOnlyMemory<byte> GetDatas()
        {
            return new ReadOnlyMemory<byte>(orginal, 0, offset);
        }
    }
}
