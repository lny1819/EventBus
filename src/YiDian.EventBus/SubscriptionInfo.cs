using System;

namespace YiDian.EventBus
{
    public class SubscriptionInfo
    {
        public bool IsDynamic { get; }
        public Type HandlerType { get; }
        public FastInvokeHandler Handler { get; }

        private SubscriptionInfo(bool isDynamic, Type handlerType, FastInvokeHandler handler)
        {
            IsDynamic = isDynamic;
            HandlerType = handlerType;
            Handler = handler;
        }

        public static SubscriptionInfo Dynamic(Type handlerType, FastInvokeHandler handler)
        {
            return new SubscriptionInfo(true, handlerType, handler);
        }
        public static SubscriptionInfo Typed(Type handlerType, FastInvokeHandler handler)
        {
            return new SubscriptionInfo(false, handlerType, handler);
        }
    }
}
