using Newtonsoft.Json;
using System;
using YiDian.EventBus;
using YiDian.EventBus.MQ;

namespace ConsoleApp
{
    public class MySeralize : IEventSeralize
    {
        public T DeserializeBytes<T>(byte[] datas) where T : IntegrationMQEvent
        {
            throw new NotImplementedException();
        }

        public T DeserializeSegment<T>(ArraySegment<byte> datas) where T : IntegrationMQEvent
        {
            throw new NotImplementedException();
        }

        public T DeserializeString<T>(string datas) where T : IntegrationMQEvent
        {
            return (T)JsonConvert.DeserializeObject(datas, typeof(T));
        }

        public ArraySegment<byte> SerializeArraySegment<T>(T @event) where T : IntegrationMQEvent
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeBytes<T>(T @event) where T : IntegrationMQEvent
        {
            throw new NotImplementedException();
        }

        public string SerializeString<T>(T @event) where T : IntegrationMQEvent
        {
            return JsonConvert.SerializeObject(@event);
        }
    }
}
