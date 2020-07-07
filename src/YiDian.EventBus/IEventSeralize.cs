using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        ReadOnlyMemory<byte> Serialize<T>(T @event);
        ReadOnlyMemory<byte> Serialize(object @event, Type type);
        int Serialize(object @event, Type type, byte[] bs, int offset);
        object DeserializeObject(ReadOnlyMemory<byte> data, Type type);
    }
}
