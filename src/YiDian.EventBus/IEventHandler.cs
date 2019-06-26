
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IEventHandler<in TIntegrationEvent> : IEventHandler 
        where TIntegrationEvent: IMQEvent
    {
        Task<bool> Handle(TIntegrationEvent @event);
    }

    public interface IEventHandler
    {
    }
}
