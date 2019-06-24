using System;
using System.IO;

namespace YiDian.EventBus
{
    public interface IEventSeralize
    {
        void Serialize<T>(MemoryStream ms, T @event) where T : IntegrationMQEvent;
        T Deserialize<T>(MemoryStream ms) where T : IntegrationMQEvent;
    }
}
