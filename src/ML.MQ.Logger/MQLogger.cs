using System;
using Microsoft.Extensions.Logging;
using ML.MqLogger.MQLogsEventBus.Abstractions;
using ML.MqLogger.Logmodel;
using ML.EventBus;
using System.Text;
using Newtonsoft.Json;

namespace ML.MqLogger
{
    internal class MQLogger : ILogger, IDisposable
    {
        public ILogEventBus EventBus { get; }

        public MQLogger(ILogEventBus eventBus)
        {
            EventBus = eventBus;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }
        public bool IsEnabled(LogLevel logLevel)
        {
            return LogLevel.Critical <= logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var data = state as LoggerEvent;
            if (data == null) return;
            EventBus.Publish(data);
        }
    }
}
namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtend
    {
        static readonly EventId NULL = new EventId(0, "");
        public static void MLLogCtritical<T>(this ILogger logger, T t, string errmsg, Exception exception = null) where T : IntegrationMQEvent
        {
            var item = new LoggerEvent() { Item = t, ErrMsg = errmsg, DateTime = DateTime.Now };
            logger.Log(LogLevel.Critical, NULL, item, exception, Formator);
        }
        static string Formator(LoggerEvent o, Exception j)
        {
            var sb = new StringBuilder();
            if (o != null)
            {
                sb.Append(o.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append("_");
                sb.Append(o.ErrMsg);
                sb.AppendLine();
                sb.Append(ToJson(o.Item));
                sb.AppendLine();
            }
            if (j != null)
            {
                sb.Append(j.ToString());
            }
            return sb.ToString();
        }

        static string ToJson(object o)
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
            else value = JsonConvert.SerializeObject(o);
            return value;
        }
    }
}
