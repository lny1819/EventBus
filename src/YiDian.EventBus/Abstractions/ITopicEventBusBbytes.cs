using YiDian.EventBus;
using YiDian.EventBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.Abstractions
{
    public interface ITopicEventBusBbytes
    {
        void DeleteQueue(string queuename, bool force);
        /// <summary>
        /// 订阅数据
        /// <para>每个协议类型可以定义约束头</para>
        /// 也可以定义约束键，发送的时候使用“约束头+顺序约束键”拼接作为key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;
        void Publish(string key, byte[] datas);
        void Subscribe(string key);
        void UnSubscribe(string key);
        void StartConsumer(string queuename, Action<ITopicEventBusBbytes> action, ushort fetchcount = 200, int length = 200000, bool autodelete = false, bool durable = true, bool autoAck = false);
    }
}