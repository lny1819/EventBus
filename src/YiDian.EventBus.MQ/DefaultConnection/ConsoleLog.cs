using Microsoft.Extensions.Logging;
using System;

namespace YiDian.EventBus.MQ.DefaultConnection
{
    internal class ConsoleLog : ILogger, IDisposable
    {
        private string __format<T>(T msg, Exception ex)
        {
            return msg.ToString() + ex.ToString();
        }
        public void LogWarning(string msg)
        {
            Log(LogLevel.Warning, new EventId(), msg, null, __format);
        }
        public void LogCritical(string msg)
        {
            Log(LogLevel.Critical, new EventId(), msg, null, __format);
        }
        public void LogInformation(string msg)
        {
            Log(LogLevel.Information, new EventId(), msg, null, __format);
        }
        string _tag = "{0}: ";
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string tag = "";
            switch (logLevel)
            {
                case LogLevel.Critical:
                    tag = string.Format(_tag, "crit");
                    break;
                case LogLevel.Debug:
                    tag = string.Format(_tag, "debug");
                    break;
                case LogLevel.Error:
                    tag = string.Format(_tag, "err");
                    break;
                case LogLevel.Information:
                    tag = string.Format(_tag, "info");
                    break;
                case LogLevel.Trace:
                    tag = string.Format(_tag, "trace");
                    break;
                case LogLevel.Warning:
                    tag = string.Format(_tag, "warn");
                    break;
                default:
                    return;
            }
            Console.WriteLine(tag + formatter(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
