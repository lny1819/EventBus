using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        void Serialize<T>(MemoryStream ms, T @event) where T : IMQEvent;
        void SerializeObject(MemoryStream ms, object @event);
        T Deserialize<T>(MemoryStream ms) where T : IMQEvent;
        object DeserializeObject(MemoryStream ms,Type type);
    }
}
