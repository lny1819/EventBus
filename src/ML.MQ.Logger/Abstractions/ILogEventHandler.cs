using ML.MqLogger.Logmodel;
using System;
using System.Threading.Tasks;

namespace ML.MqLogger.Abstractions
{
    public interface ILogEventHandler<in TIntegrationEvent> : ILogEventHandler
    {
        Task Handle(TIntegrationEvent @event, DateTime date, string errmsg);
    }

    public interface ILogEventHandler
    {
    }
}
