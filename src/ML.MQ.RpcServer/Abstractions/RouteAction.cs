using ML.MQ.RpcServer.Route;
using System;
using static ML.EventBus.FastInvoke;

namespace ML.MQ.RpcServer.Abstractions
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
