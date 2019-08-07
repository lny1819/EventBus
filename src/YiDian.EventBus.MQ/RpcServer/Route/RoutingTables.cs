using YiDian.EventBus.MQ.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace YiDian.EventBus.MQ.Route
{
    internal class RoutingTables
    {
        const string NOT_FOUND = "未找到匹配的控制器或方法";
        /// <summary>
        /// Action名称字典 Action名称=methodInfo
        /// </summary>
        static Dictionary<string, ActionInfo> ActionsDic = new Dictionary<string, ActionInfo>(StringComparer.OrdinalIgnoreCase);

        public static void LoadControlers(string appId)
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
                        //if (!m.ReturnType.IsGenericType || m.ReturnType.GetGenericTypeDefinition() != typeof(Task<>)
                        //      || m.ReturnType.GenericTypeArguments.Length == 0
                        //      || m.ReturnType.GenericTypeArguments[0].GetGenericTypeDefinition() != typeof(ActionResult<>)
                        //      ) continue;
                        if (!m.ReturnType.IsGenericType
                            || m.ReturnType.GenericTypeArguments.Length == 0
                            || m.ReturnType.GetGenericTypeDefinition() != typeof(ActionResult<>)
                            ) continue;
                        var sb = new StringBuilder();
                        sb.Append(appId);
                        sb.Append('.');
                        sb.Append(typename.Substring(0, length));
                        sb.Append('.');
                        sb.Append(m.Name);
                        var key = sb.ToString();
                        var info = new ActionInfo()
                        {
                            ControllerType = t,
                            //Method =GetMethodInvoker(m),
                            AaguType = m.GetParameters().Length > 0 ? m.GetParameters()[0].ParameterType : null,
                            ReturnType = m.ReturnType
                        };
                        ActionsDic.Add(key, info);
                    }
                }
            }
        }

        public static RouteAction Route(string routingKey, string appid, out string msg)
        {
            if (!ActionsDic.TryGetValue(routingKey, out ActionInfo action))
            {
                msg = NOT_FOUND;
                return null;
            };
            msg = string.Empty;
            return new RouteAction(action)
            {
                RequestUri = routingKey
            };
        }
    }


    internal class ActionInfo
    {
        public Type ControllerType { get; set; }
        public FastInvokeHandler Method { get; set; }
        public Type ReturnType { get; set; }
        public Type AaguType { get; set; }
    }
}
