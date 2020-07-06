using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        byte[] Serialize<T>(T @event) where T : IMQEvent;
        object DeserializeObject(byte[] data, Type type);
        object DeserializeObject(byte[] data, Type type, int index, int count);
    }
}
