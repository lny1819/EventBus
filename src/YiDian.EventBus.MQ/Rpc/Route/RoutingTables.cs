using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using YiDian.EventBus.MQ.KeyAttribute;
using YiDian.EventBus.MQ.Rpc.Abstractions;

namespace YiDian.EventBus.MQ.Rpc.Route
{
    internal class RoutingTables
    {
        const string NOT_FOUND = "未找到匹配的控制器或方法";
        /// <summary>
        /// Action名称字典 Action名称=methodInfo
        /// </summary>
        Dictionary<string, ActionInfo> actionsDic = new Dictionary<string, ActionInfo>(StringComparer.OrdinalIgnoreCase);

        public void LoadControlers(string appId)
        {
            var ass = Assembly.GetEntryAssembly();
            foreach (var t in ass.GetTypes())
            {
                if (t.IsSubclassOf(typeof(RpcController)))
                {
                    var typename = t.Name;
                    var length = typename.LastIndexOf("controller", StringComparison.OrdinalIgnoreCase);
                    if (length == -1) continue;
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        if (!m.ReturnType.IsGenericType || m.ReturnType.GenericTypeArguments.Length == 0 || m.ReturnType.GetGenericTypeDefinition() != typeof(ActionResult<>)) continue;
                        var sb = new StringBuilder();
                        sb.Append('/');
                        sb.Append(typename.Substring(0, length));
                        sb.Append('/');
                        sb.Append(m.Name);
                        var key = sb.ToString();
                        var info = new ActionInfo()
                        {
                            ControllerType = t,
                            Method = FastInvoke.GetMethodInvoker(m),
                            InAags = m.GetParameters(),
                            ReturnType = m.ReturnType
                        };
                        actionsDic.Add(key, info);
                    }
                }
            }
        }

        public ActionInfo Route(string routingKey)
        {
            if (!actionsDic.TryGetValue(routingKey, out ActionInfo action))
            {
                return null;
            };
            return action;
        }
    }


    public class ActionInfo
    {
        public Type ControllerType { get; set; }
        public FastInvokeHandler Method { get; set; }
        public Type ReturnType { get; set; }
        public ParameterInfo[] InAags { get; set; }
    }
}
