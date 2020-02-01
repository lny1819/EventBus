using System.Text.Json;
using System.Text.Json.Serialization;

namespace System
{
    internal static class JsonHelper
    {
        static readonly JsonSerializerOptions options = new JsonSerializerOptions();
        static string s_default_dateFormat = "yyyy-MM-dd HH:mm:ss";
        static string __date_format;
        static JsonHelper()
        {
            DateFormat = s_default_dateFormat;
        }
        public static string DateFormat
        {
            get { return __date_format; }
            set
            {
                lock (options)
                {
                    UseUtcKindDataParse();
                    if (string.IsNullOrEmpty(value)) __date_format = s_default_dateFormat;
                    else __date_format = value;
                    var format = new DateParse(__date_format);
                    options.Converters.Add(format);
                }
            }
        }
        public static string ToJson(this object o, JsonSerializerOptions toJsonOptions = null)
        {
            if (o == null) return "";
            string value;
            var type = o.GetType();
            if (type == typeof(String))
            {
                value = o.ToString();
            }
            else if (type.IsEnum) value = ((int)o).ToString();
            else if (type.IsValueType) value = o.ToString();
            else value = JsonSerializer.Serialize(o, toJsonOptions ?? options);
            return value;
        }
        public static object JsonTo(this string JsonStr, Type type, JsonSerializerOptions toJsonOptions = null)
        {
            if (string.IsNullOrEmpty(JsonStr))
            {
                return null;
            }
            if (type == typeof(String))
            {
                return JsonStr;
            }
            else if (type.IsEnum)
            {
                return Enum.Parse(type, JsonStr);
            }
            else
            {
                return JsonSerializer.Deserialize(JsonStr, type, toJsonOptions ?? options);
            }
        }
        public static T JsonTo<T>(this string JsonStr, JsonSerializerOptions toJsonOptions = null)
        {
            if (string.IsNullOrEmpty(JsonStr))
            {
                return default(T);
            }
            var type = typeof(T);
            if (type == typeof(String))
            {
                object o = JsonStr;
                return (T)o;
            }
            else if (type.IsEnum)
            {
                return (T)Enum.Parse(type, JsonStr);
            }
            else
            {
                return (T)JsonSerializer.Deserialize(JsonStr, type, toJsonOptions ?? options);
            }
        }
        /// <summary>
        /// format:2020-01-08T18:57:12.629+08:00
        /// </summary>
        public static void UseUtcKindDataParse()
        {
            DateParse dt = null;
            foreach (var item in options.Converters)
            {
                dt = item as DateParse;
                if (dt != null) break;
            }
            if (dt != null) options.Converters.Remove(dt);
        }
        public static void SetDefaultDateParse(this JsonSerializerOptions serializerOptions,string format)
        {
            DateParse dt = null;
            foreach (var item in serializerOptions.Converters)
            {
                dt = item as DateParse;
                if (dt != null) break;
            }
            if (dt != null) serializerOptions.Converters.Remove(dt);
            var f = new DateParse(format);
            options.Converters.Add(f);

        }
        private class DateParse : JsonConverter<DateTime>
        {
            public DateParse(string format)
            {
                FormatSettings = format;
            }
            public string FormatSettings { get; }
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTime.ParseExact(reader.GetString(), FormatSettings, System.Globalization.CultureInfo.CurrentCulture);
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(FormatSettings));
            }
        }
    }
}
