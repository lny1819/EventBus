using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        byte[] Serialize<T>(T @event) where T : IMQEvent;
        object DeserializeObject(byte[] data, Type type);
    }
}
