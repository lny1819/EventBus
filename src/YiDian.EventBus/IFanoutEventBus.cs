using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    /// <summary>
    /// 定义广播类型的消息总线
    /// </summary>
    public interface IFanoutEventBus : IEventBus
    {
        /// <summary>
        /// 注册消息队列机器队列消费消息方法
        /// </summary>
        /// <param name="queuename">消费队列名称</param>
        /// <param name="action">订阅队列消费消息类型</param>
        /// <param name="fetchCount">每次最多获取消息数量</param>
        /// <param name="queueLength">队列最大数据存储长度</param>
        /// <param name="autodel">是否自动删除队列</param>
        /// <param name="durable">队列数据是否持久化</param>
        /// <param name="autoAck">队列数据是否自动确认</param>
        /// <param name="autoStart">是否在注册完成后开启消息消费</param>
        void RegisterConsumer(string queuename, Action<FanoutSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = true, bool durable = false, bool autoAck = true, bool autoStart = true);
    }
}
