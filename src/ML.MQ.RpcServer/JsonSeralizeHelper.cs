using Newtonsoft.Json;
using System;
using System.Text;

namespace ML.MQ.RpcServer
{
    internal static class SeralizeHelper
    {
        public static byte[] SeralizeAndGetBytes(this object o, Encoding encode)
        {
            string value;
            var type = o.GetType();
            if (type == typeof(String))
            {
                value = o.ToString();
            }
            else if (type.IsEnum) value = ((int)o).ToString();
            else if (type.IsValueType) value = o.ToString();
            else value = JsonConvert.SerializeObject(o);
            if (string.IsNullOrEmpty(value)) return new byte[0];
            return encode.GetBytes(value);
        }
        public static T DeseralizeBytes<T>(this byte[] data, Encoding encode)
        {
            var type = typeof(T);
            return (T)DeseralizeBytes(data, type, encode);
        }
        public static object DeseralizeBytes(this byte[] data, Type type, Encoding encode)
        {
            string value = encode.GetString(data);
            if (type == typeof(String))
            {
                return value;
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }
            else
            {
                return JsonConvert.DeserializeObject(value, type);
            }
        }
        public static object DeseralizeBytes(this byte[] data, int offset, int count, Type type, Encoding encode)
        {
            string value = encode.GetString(data, offset, count);
            if (type == typeof(String))
            {
                return value;
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }
            else
            {
                return JsonConvert.DeserializeObject(value, type);
            }
        }
    }
}
