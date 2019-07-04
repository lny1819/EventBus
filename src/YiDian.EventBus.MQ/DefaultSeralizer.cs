using System;
using System.Collections.Generic;
using System.Text;

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
            obj.BytesTo(readstream);
            return obj;
        }

        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            var obj = (IYiDianSeralize)@event as IYiDianSeralize;
            var write = new WriteStream(2000);
            obj.ToBytes(write);
            return write.GetBytes();
        }
    }
}
