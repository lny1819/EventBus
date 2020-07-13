using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public class TextSeralize : IEventSeralize
    {
        readonly Encoding encoding;
        public TextSeralize(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            return encoding.GetString(data.Span);
        }

        public uint GetSize(object obj, Type type)
        {
            return (uint)encoding.GetByteCount(obj.ToString());
        }

        public ReadOnlyMemory<byte> Serialize<T>(T @event)
        {
            return encoding.GetBytes(@event.ToString());
        }

        public ReadOnlyMemory<byte> Serialize(object @event, Type type)
        {
            return encoding.GetBytes(@event.ToString());
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
