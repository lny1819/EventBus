using System;

namespace YiDian.EventBus.MQ
{
    public interface ISeralize
    {
        string SerializeObject<T>(T @event) where T : IntegrationMQEvent;
        object DeserializeObject(string v, Type type);
    }
}
