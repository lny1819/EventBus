using ML.EventBus;
using System;

namespace ML.MqLogger.Logmodel
{
    public class LoggerEvent
    {
        public IntegrationMQEvent Item { get; set; }
        public string ErrMsg { get; set; }
        public DateTime DateTime { get; set; }
    }
    internal class LoggerEvent<T> where T : IntegrationMQEvent
    {
        public T Item { get; set; }
        public string ErrMsg { get; set; }
        public DateTime DateTime { get; set; }
    }
}
