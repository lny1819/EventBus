using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class JsonSralizer : IEventSeralize
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public object DeserializeObject(byte[] data, Type type)
        {
            var str = Encoding.GetString(data);
            return str.JsonTo(type);
        }

        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            var str = @event.ToJson();
            return Encoding.GetBytes(str);
        }
    }
}
