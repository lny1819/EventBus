using YiDian.EventBus.MQ.Route;
using System;

namespace YiDian.EventBus.MQ.Abstractions
{
    public class RouteAction
    {
        internal RouteAction(ActionInfo info)
        {
            ControllerType = info.ControllerType;
            CurrentMethod = info.Method;
            InArgumentType = info.AaguType;
            OutArgumentType = info.ReturnType;
        }
        public string RequestUri { get; set; }
        public Type ControllerType { get; }
        internal FastInvokeHandler CurrentMethod { get; }
        public Type InArgumentType { get; }
        public Type OutArgumentType { get; }
    }
}
