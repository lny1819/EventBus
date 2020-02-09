using System;
using System.Text;
using System.Text.Json;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// 实现IEventSeralize的Json序列化
    /// </summary>
    public class JsonSerializer : IEventSeralize
    {
        readonly JsonSerializerOptions opt;
        string dateFormat;
        /// <summary>
        /// 创建一个JSON序列化实例 
        /// </summary>
        public JsonSerializer()
        {
            opt = new JsonSerializerOptions();
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
        /// <summary>
        /// 编码格式
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        /// <summary>
        /// 反序列化字节数组
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object DeserializeObject(byte[] data, Type type)
        {
            var str = Encoding.GetString(data);
            return str.JsonTo(type, opt);
        }
        /// <summary>
        /// 序列化对象为字节数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <returns></returns>
        public byte[] Serialize<T>(T @event) where T : IMQEvent
        {
            var str = @event.ToJson(opt);
            return Encoding.GetBytes(str);
        }
    }
}
