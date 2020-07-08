using System;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class DefaultSeralizer : IEventSeralize
    {
        private Encoding encoding;

        public DefaultSeralizer(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            if (data.Length == 0) return null;
            var constructor = type.GetConstructor(Type.EmptyTypes);
            var obj = constructor.Invoke(null) as IYiDianSeralize;
            if (obj == null) throw new ArgumentNullException("the type " + type.Name + " can not be convert as " + nameof(IYiDianSeralize));
            var readstream = new ReadStream(data) { Encoding = encoding };
            obj.BytesTo(readstream);
            return obj;
        }

        public ReadOnlyMemory<byte> Serialize<T>(T @event)
        {
            return Serialize(@event, typeof(T));
        }

        public ReadOnlyMemory<byte> Serialize(object obj, Type type)
        {
            if (type.IsArray)
            {

            }
            if (!(obj is IMQEvent)) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (!(obj is IYiDianSeralize seralize)) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var write = new WriteStream(2000) { Encoding = encoding };
            seralize.ToBytes(write);
            return write.GetBytes();
        }

        public int Serialize(object obj, Type type, byte[] bs, int offset)
        {
            bool isArray = false;
            var ilist = type.GetInterface(typeof(System.Collections.Generic.IList<>).FullName);
            if (ilist != null)
            {
                isArray = true;
                type = ilist.GetGenericArguments()[0];
            }
            if (type.GetInterface(typeof(IMQEvent).FullName) == null) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (type.GetInterface(typeof(IYiDianSeralize).FullName) == null) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var stream = new WriteStream(bs, offset) { Encoding = encoding };
            if (!isArray)
            {
                var seralize = (IYiDianSeralize)obj;
                return (int)seralize.ToBytes(stream);
            }
            if (type.IsValueType)
            {
                if (type == typeof(byte)) stream.WriteArrayByte((byte[])obj);
            }
            else if (type == typeof(string))
            {

            }
        }
    }
}
