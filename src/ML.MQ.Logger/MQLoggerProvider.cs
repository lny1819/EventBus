using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ML.MqLogger.MQLogsEventBus.Abstractions;
using System;
using System.Collections.Concurrent;

namespace ML.MqLogger
{
    public class MQLoggerProvider : ILoggerProvider
    {
        readonly MQLogger logger;
        public MQLoggerProvider(IServiceProvider sp)
        {
            var eventBus = sp.GetRequiredService<ILogEventBus>();
            logger = new MQLogger(eventBus);
        }
        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
        }
    }
}
