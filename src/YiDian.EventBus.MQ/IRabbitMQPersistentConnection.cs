using RabbitMQ.Client;
using System;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// MQ连接
    /// </summary>
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        /// <summary>
        /// 连接失败
        /// </summary>
        event EventHandler<string> ConnectFail;
        /// <summary>
        /// 连接名称
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 是否连接成功
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// 订阅管理器工厂
        /// </summary>
        IEventBusSubManagerFactory SubsFactory { get; }
        /// <summary>
        /// 重连
        /// </summary>
        /// <returns></returns>
        bool TryConnect();
        /// <summary>
        /// 创建Channel
        /// </summary>
        /// <returns></returns>
        IModel CreateModel();
    }
}
