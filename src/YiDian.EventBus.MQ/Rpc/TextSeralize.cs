using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    /// <summary>
    /// 
    /// </summary>
    public class TextSeralize : IEventSeralize
    {
        readonly Encoding encoding;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoding"></param>
        public TextSeralize(Encoding encoding)
        {
            this.encoding = encoding;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            return encoding.GetString(data.Span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public uint GetSize(object obj, Type type)
        {
            return (uint)encoding.GetByteCount(obj.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <returns></returns>
        public ReadOnlyMemory<byte> Serialize<T>(T @event)
        {
            return encoding.GetBytes(@event.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ReadOnlyMemory<byte> Serialize(object @event, Type type)
        {
            return encoding.GetBytes(@event.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="type"></param>
        /// <param name="bs"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
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
