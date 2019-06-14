
using Autofac;
using Microsoft.Extensions.Logging;
using ML.EventBus;
using ML.EventBusMQ;
using ML.MqLogger;
using ML.MqLogger.MQLogsEventBus;
using ML.MqLogger.MQLogsEventBus.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RabbitMqExtesions
    {
        /// <summary>
        /// 依赖 IRabbitMQPersistentConnection 组件
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMQLogger(this IServiceCollection services)
        {
            services
            .AddSingleton<ILogEventBus, LogEventBusMQ>(sp =>
            {
                var conn = sp.GetService<IRabbitMQPersistentConnection>();
                var scope = sp.GetService<ILifetimeScope>();
                var eventbus = new LogEventBusMQ(conn, scope);
                return eventbus;
            }).AddSingleton<ILoggerProvider>(sp =>
            {
                return new MQLoggerProvider(sp);
            });
            return services;
        }
    }
}
