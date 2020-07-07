using YiDian.EventBus.MQ.Rpc.Route;
using System;
using System.Reflection;

namespace YiDian.EventBus.MQ.Rpc.Abstractions
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
        public ParameterInfo[] InArgumentType { get; }
        public Type OutArgumentType { get; }
    }
}
