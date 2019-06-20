using System;

namespace YiDian.EventBus
{
    public class SubscriptionInfo
    {
        public bool IsDynamic { get; }
        public Type HandlerType { get; }
        public Type EventType { get; }
        public FastInvokeHandler Handler { get; }

        private SubscriptionInfo(bool isDynamic, Type handlerType, Type eventType, FastInvokeHandler handler)
        {
            IsDynamic = isDynamic;
            HandlerType = handlerType;
            Handler = handler;
            EventType = eventType;
        }
        public static SubscriptionInfo Dynamic(Type handlerType, FastInvokeHandler handler)
        {
            return new SubscriptionInfo(true, handlerType, null, handler);
        }
        public static SubscriptionInfo Typed(Type handlerType, Type eventType, FastInvokeHandler handler)
        {
            return new SubscriptionInfo(false, handlerType, eventType, handler);
        }
    }
}
