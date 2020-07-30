using System;
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    /// <summary>
    /// 定义动态消息回调函数接口
    /// </summary>
    public interface IBytesHandler : IEventHandler
    {
        /// <summary>
        /// 动态消息回调处理方法
        /// </summary>
        /// <param name="routingKey">消息路由键 ，为_dy或者以_dy.开始</param>
        /// <param name="datas">消息字节数据</param>
        /// <returns></returns>
        Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas);
    }
}
