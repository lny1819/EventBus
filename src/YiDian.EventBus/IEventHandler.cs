
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    /// <summary>
    /// 定义消息回调函数接口
    /// </summary>
    /// <typeparam name="TIntegrationEvent"></typeparam>
    public interface IEventHandler<in TIntegrationEvent> : IEventHandler 
        where TIntegrationEvent: IMQEvent
    {
        /// <summary>
        /// 消息回调处理方法
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        Task<bool> Handle(TIntegrationEvent @event);
    }
    /// <summary>
    /// 消息回调接口
    /// </summary>
    public interface IEventHandler
    {
    }
}
