using System;
using System.Collections.Generic;
using System.Linq;

namespace YiDian.EventBus
{
    public class SubManagerA : ISubHandler
    {
        public bool IsEmpty => !_keyTypes.Any();

        private readonly HashSet<string> _keyTypes;
        private readonly List<Type> _eventTypes;
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

        public SubManagerA()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>(StringComparer.OrdinalIgnoreCase);
            _keyTypes = new HashSet<string>();
            _eventTypes = new List<Type>();
        }

        internal void RemoveHandlers(string key)
        {
            _keyTypes.Remove(key);
        }
        public void AddSubscription<T, TH>(string key)
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            if (!_keyTypes.Contains(key)) _keyTypes.Add(key);
            if (!_eventTypes.Exists(e => e == typeof(T))) _eventTypes.Add(typeof(T));
            var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
            var handler = FastInvoke.GetMethodInvoker(method);
            DoAddSubscription(typeof(TH), handler, GetEventKey<T>());
        }
        private void DoAddSubscription(Type handlerType, FastInvokeHandler handler, string eventName)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                return;
            }
            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType, handler));
        }

        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public void Clear()
        {
            _keyTypes.Clear();
            _handlers.Clear();
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            if (!_handlers.TryGetValue(eventName, out List<SubscriptionInfo> res)) res = new List<SubscriptionInfo>();
            return res;
        }

        public string GetEventKey<T>()
        {
            return typeof(T).FullName.ToLower();
        }

        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => string.Equals(t.FullName, eventName, StringComparison.OrdinalIgnoreCase));
    }
}
