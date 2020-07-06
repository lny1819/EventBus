using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public class TextSeralize : IEventSeralize
    {
        public TextSeralize(Encoding encoding)
        {

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

        public int Serialize(object @event, Type type, byte[] bs, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
