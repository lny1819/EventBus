using Newtonsoft.Json;
using System;
using YiDian.EventBus;
using YiDian.EventBus.MQ;

namespace ConsoleApp
{
    public class MySeralize : ISeralize
    {
        public object DeserializeObject(string v, Type type)
        {
            return JsonConvert.DeserializeObject(v, type);
        }

        public string SerializeObject<T>(T @event) where T : IntegrationMQEvent
        {
            return JsonConvert.SerializeObject(@event);
        }
    }
}
