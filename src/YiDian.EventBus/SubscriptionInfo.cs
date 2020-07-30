using System;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息订阅信息
    /// </summary>
    public class SubscriptionInfo
    {
        /// <summary>
        /// 是否是动态消息
        /// </summary>
        public bool IsDynamic { get; }
        /// <summary>
        /// 消息回调类型
        /// </summary>
        public Type HandlerType { get; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public Type EventType { get; }
        /// <summary>
        /// 消息消费方法
        /// </summary>
        public FastInvokeHandler Handler { get; }
        /// <summary>
        /// 订阅KEY
        /// </summary>
        public string SubKey { get; }
        /// <summary>
        /// 消息KEY
        /// </summary>
        public string EventKey { get; }
        /// <summary>
        /// 交换机名称
        /// </summary>
        public string BrokerName { get; }

        private SubscriptionInfo(string subkey, string eventKey, string borkerName, bool isDynamic, Type handlerType, Type eventType, FastInvokeHandler handler)
        {
            SubKey = subkey;
            EventKey = eventKey;
            IsDynamic = isDynamic;
            HandlerType = handlerType;
            Handler = handler;
            EventType = eventType;
            BrokerName = borkerName;
        }
        /// <summary>
        /// 创建动态消息
        /// </summary>
        /// <param name="subkey"></param>
        /// <param name="eventKey"></param>
        /// <param name="handlerType"></param>
        /// <param name="handler"></param>
        /// <param name="borkerName"></param>
        /// <returns></returns>
        public static SubscriptionInfo Dynamic(string subkey, string eventKey, Type handlerType, FastInvokeHandler handler, string borkerName)
        {
            return new SubscriptionInfo(subkey, eventKey, borkerName, true, handlerType, null, handler);
        }
        /// <summary>
        /// 创建事件消息
        /// </summary>
        /// <param name="subkey"></param>
        /// <param name="eventKey"></param>
        /// <param name="handlerType"></param>
        /// <param name="eventType"></param>
        /// <param name="handler"></param>
        /// <param name="borkerName"></param>
        /// <returns></returns>
        public static SubscriptionInfo Typed(string subkey, string eventKey, Type handlerType, Type eventType, FastInvokeHandler handler, string borkerName)
        {
            return new SubscriptionInfo(subkey, eventKey, borkerName, false, handlerType, eventType, handler);
        }
    }
}
