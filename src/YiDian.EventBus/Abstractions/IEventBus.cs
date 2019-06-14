using System;

namespace YiDian.EventBus.Abstractions
{
    public interface IEventBus
    {
        /// <summary>
        /// 设置每个消息创建类型实例的缓存数量
        /// <para>当设置为0时表示不缓存</para>
        /// 设置缓存数量可以有效降低di容器创建类型实例的CPU消耗
        /// </summary>
        /// <param name="cacheLength"></param>
        void EnableHandlerCache(int cacheLength);
        event Action<AggregateException> ProcessException;
        void StartConsumer(string queuename, Action<IEventBus> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = false, bool durable = true, bool autoAck = false);
        void DeleteQueue(string queuename, bool force);
        void Publish<T>(T @event) where T : IntegrationMQEvent;

        void Subscribe<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>;

        void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent;
    }
}
