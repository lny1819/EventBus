using System;
using static YiDian.EventBus.FastInvoke;

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

        public static SubscriptionInfo Dynamic(Type handlerType)
        {
            return new SubscriptionInfo(true, handlerType, null);
        }
        public static SubscriptionInfo Typed(Type handlerType, FastInvokeHandler handler)
        {
            return new SubscriptionInfo(false, handlerType, handler);
        }
    }
}
