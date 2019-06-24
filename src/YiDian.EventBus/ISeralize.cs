using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        void Serialize<T>(MemoryStream ms, T @event) where T : IntegrationMQEvent;
        T DeserializeBytes<T>(MemoryStream ms) where T : IntegrationMQEvent;
    }
}
