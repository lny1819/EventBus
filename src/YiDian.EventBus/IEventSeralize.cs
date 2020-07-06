using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        byte[] Serialize<T>(T @event) where T : IMQEvent;
        byte[] Serialize(object @event, Type type);
        int Serialize(object @event, Type type, byte[] bs, int offset);
        object DeserializeObject(byte[] data, Type type);
        object DeserializeObject(byte[] data, Type type, int index, int count);
    }
}
