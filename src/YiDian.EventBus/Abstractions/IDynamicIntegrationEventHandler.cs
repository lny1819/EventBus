using System.Threading.Tasks;

namespace YiDian.EventBus.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(string eventData);
    }
}
