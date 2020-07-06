using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public class TextSeralize : IEventSeralize
    {
        Encoding encoding;
        public TextSeralize(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize(object @event, Type type)
        {
            throw new NotImplementedException();
        }

        public unsafe int Serialize(object @event, Type type, byte[] bs, int offset)
        {
            var value = @event.ToString();
            var length = encoding.GetByteCount(value);
            var span = new Span<byte>(bs, offset, length);
            fixed (char* cPtr = @event.ToString())
            {
                fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                {
                    return encoding.GetBytes(cPtr, value.Length, bPtr, length);
                }
            }
        }
    }
}
