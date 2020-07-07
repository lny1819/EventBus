using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
                    var ctl_name = typename.Substring(0, length);
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        if (m.ReturnType.IsGenericType)
                        {
                            if (m.ReturnType.GetGenericTypeDefinition() == typeof(ActionResult<>)) Add(m, ctl_name, t, false);
                            else if (m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                            {
                                if (m.ReturnType.GenericTypeArguments[0].Name == typeof(ActionResult<>).Name) Add(m, ctl_name, t, true);
                            }
                        }
                    }
                }
            }
        }

        private void Add(MethodInfo m, string ctl_name, Type ctl_type, bool isTask)
        {
            var sb = new StringBuilder();
            sb.Append('/');
            sb.Append(ctl_name);
            sb.Append('/');
            sb.Append(m.Name);
            var key = sb.ToString();
            var info = new ActionInfo()
            {
                ControllerType = ctl_type,
                Method = FastInvoke.GetMethodInvoker(m),
                InAags = m.GetParameters()
            };
            info.SetReturnType(m.ReturnType, isTask);
            if (info.IsTask) info.SetResGetter();
            actionsDic.Add(key, info);
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


    internal class ActionInfo
    {
        Func<object, object> getter;
        internal Type ControllerType { get; set; }
        internal FastInvokeHandler Method { get; set; }
        internal Type ReturnType { get; private set; }
        internal ParameterInfo[] InAags { get; set; }
        internal bool IsTask { get; private set; }

        internal object GetTaskResult(object obj)
        {
            if (getter == null) throw new ArgumentNullException(nameof(getter), "used for task result");
            return getter(obj);
        }
        internal Type ActionResultType { get; }
        internal void SetResGetter()
        {
            var e = ReturnType.GetProperty("Result");
            getter = FastInvoke.EmitGetter(e);
        }
        internal void SetReturnType(Type returnType, bool isTask)
        {
            ReturnType = returnType;
            IsTask = isTask;
            if (isTask)
            {
                ActionResultType = returnType.GenericTypeArguments[0].GenericTypeArguments[0];
            }
            else
            {
                ActionResultType = returnType.GenericTypeArguments[0];
            }
        }
    }
}
