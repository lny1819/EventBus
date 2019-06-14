using ML.MqLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using ML.EventBus;
using static ML.EventBus.FastInvoke;

namespace ML.MqLogger.MQLogsEventBus
{
    public class SubInfo
    {
        public Type SubType { get; set; }
        public FastInvokeHandler Method { get; set; }
    }
    public class LogEventBusMQSubManager : ILogEventBusSubMgr
    {
        private readonly Dictionary<string, Tuple<Type, List<SubInfo>>> _handlers;
        public event EventHandler<string> OnEventRemoved;

        public LogEventBusMQSubManager()
        {
            _handlers = new Dictionary<string, Tuple<Type, List<SubInfo>>>(StringComparer.OrdinalIgnoreCase);
        }
        public bool IsEmpty => !_handlers.Keys.Any();
        public IEnumerable<SubInfo> GetHandlersForEvent(string eventName, out Type eventType)
        {
            if (_handlers.ContainsKey(eventName))
            {
                var data = _handlers[eventName];
                eventType = data.Item1;
                return data.Item2;
            }
            eventType = null;
            return new List<SubInfo>();
        }
        private void RaiseOnEventRemoved(string eventName)
        {
            OnEventRemoved?.Invoke(this, eventName);
        }

        public bool HasSubscriptionsForEvent(string name)
        {
            return _handlers.ContainsKey(name);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : ILogEventHandler<T>
        {
            var name = typeof(T).FullName;
            var type = typeof(TH);
            var method = type.GetMethod("Handle", new Type[] { typeof(T),typeof(DateTime),typeof(string) });
            var handler = GetMethodInvoker(method);
            var subinfo = new SubInfo() { Method = handler, SubType = type };
            if (HasSubscriptionsForEvent(name))
            {
                if (!_handlers[name].Item2.Any(e => e.SubType == type))
                    _handlers[name].Item2.Add(subinfo);
            }
            else
            {
                _handlers.Add(name, new Tuple<Type, List<SubInfo>>(typeof(T), new List<SubInfo>()));
                _handlers[name].Item2.Add(subinfo);
            }
        }

        public void RemoveSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : ILogEventHandler<T>
        {
            var name = typeof(T).FullName;
            var type = typeof(TH);
            var subinfo = _handlers[name].Item2.FirstOrDefault(e => e.SubType == type);
            _handlers[name].Item2.Remove(subinfo);
            if (!_handlers[name].Item2.Any())
            {
                _handlers.Remove(name);
                RaiseOnEventRemoved(name);
            }
        }
    }
}
