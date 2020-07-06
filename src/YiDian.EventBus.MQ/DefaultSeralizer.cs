using System;

namespace YiDian.EventBus.MQ
{
    public class DefaultSeralizer : IEventSeralize
    {
        public object DeserializeObject(byte[] data, Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            var obj = constructor.Invoke(null) as IYiDianSeralize;
            if (obj == null) throw new ArgumentNullException("the type " + type.Name + " can not be convert as " + nameof(IYiDianSeralize));
            var readstream = new ReadStream(data);
            obj.BytesTo(ref readstream);
            return obj;
        }

        public object DeserializeObject(byte[] data, Type type, int index, int count)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            var obj = constructor.Invoke(null) as IYiDianSeralize;
            if (obj == null) throw new ArgumentNullException("the type " + type.Name + " can not be convert as " + nameof(IYiDianSeralize));
            var readstream = new ReadStream(data, index);
            obj.BytesTo(ref readstream);
            return obj;
        }

        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            return Serialize(@event, typeof(T));
        }

        public byte[] Serialize(object obj, Type type)
        {
            if (!(obj is IMQEvent)) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (!(obj is IYiDianSeralize seralize)) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var write = new WriteStream(2000);
            seralize.ToBytes(ref write);
            return write.GetBytes();
        }

        public int Serialize(object obj, Type type, byte[] bs, int offset)
        {
            if (!(obj is IMQEvent)) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (!(obj is IYiDianSeralize seralize)) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var write = new WriteStream(bs, offset);
            return (int)seralize.ToBytes(ref write);
        }
    }
}
