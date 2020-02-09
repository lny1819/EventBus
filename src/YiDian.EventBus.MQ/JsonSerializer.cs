using System;
using System.Text;
using System.Text.Json;

namespace YiDian.EventBus.MQ
{
    public class JsonSerializer : IEventSeralize
    {
        readonly JsonSerializerOptions opt;
        string dateFormat;
        public JsonSerializer(Action<JsonSerializerOptions> options = null)
        {
            opt = new JsonSerializerOptions();
            options(opt);
        }
        /// <summary>
        /// 默认使用UtcKindDataParse
        /// </summary>
        public string DateFormat
        {
            get { return dateFormat; }
            set
            {
                dateFormat = value;
                opt.SetDefaultDateParse(value);
            }
        }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public object DeserializeObject(byte[] data, Type type)
        {
            var str = Encoding.GetString(data);
            return str.JsonTo(type, opt);
        }

        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            var str = @event.ToJson(opt);
            return Encoding.GetBytes(str);
        }
    }
}
