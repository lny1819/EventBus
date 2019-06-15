using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    public class InMemorySubFactory : IEventBusSubscriptionsManagerFactory
    {
        readonly List<InMemoryEventBusSubscriptionsManager> __containers = new List<InMemoryEventBusSubscriptionsManager>();
        public IEventBusSubscriptionsManager GetOrCreateByQueue(string queueName)
        {
            var mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
            if (mgr != null) return mgr;
            lock (__containers)
            {
                mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
                if (mgr != null) return mgr;
                mgr = new InMemoryEventBusSubscriptionsManager(queueName);
                __containers.Add(mgr);
                return mgr;
            }
        }
    }
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly ConcurrentDictionary<string, List<SubscriptionInfo>> _handlers;

        public event EventHandler<string> OnEventRemoved;

        public InMemoryEventBusSubscriptionsManager(string name)
        {
            QueueName = name;
            _handlers = new ConcurrentDictionary<string, List<SubscriptionInfo>>(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEmpty => !_handlers.Keys.Any();

        public string QueueName { get; }

        public void AddSubscription<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            var method = typeof(TH).GetMethod("Handle");
            var handler = FastInvoke.GetMethodInvoker(method);
            DoAddSubscription(typeof(TH), typeof(byte), handler, eventName, isDynamic: true);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
            var handler = FastInvoke.GetMethodInvoker(method);
            DoAddSubscription(typeof(TH), typeof(T), handler, eventName, isDynamic: false);
        }

        private void DoAddSubscription(Type handlerType, Type eventType, FastInvokeHandler handler, string eventName, bool isDynamic)
        {
            if (!_handlers.TryGetValue(eventName, out List<SubscriptionInfo> items))
            {
                lock (_handlers)
                {
                    if (!_handlers.TryGetValue(eventName, out items))
                    {
                        items = new List<SubscriptionInfo>();
                        _handlers.TryAdd(eventName, items);
                    }
                }
            }
            if (isDynamic) items.Add(SubscriptionInfo.Dynamic(handlerType, handler));
            else items.Add(SubscriptionInfo.Typed(handlerType, eventType, handler));
        }

        public void RemoveSubscription<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            DoRemoveHandler(eventName, typeof(TH));
        }

        public void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent
        {
            var eventName = GetEventKey<T>();
            DoRemoveHandler(eventName, typeof(TH));
        }
        public void RemoveSubscription<T, TH>(string eventName)
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationMQEvent
        {
            DoRemoveHandler(eventName, typeof(TH));
        }
        private void DoRemoveHandler(string eventName, Type HandlerType)
        {
            if (!_handlers.TryGetValue(eventName, out List<SubscriptionInfo> items))
                return;
            var handlers = items.Where(x => x.HandlerType == HandlerType).ToList();
            items.ForEach(x => handlers.Remove(x));
            if (items.Count == 0)
            {
                if (!_handlers.TryRemove(eventName, out items)) return;
                RaiseOnEventRemoved(eventName);
            }
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            var flag = _handlers.TryGetValue(eventName, out List<SubscriptionInfo> res);
            if (flag) return res;
            else return null;
        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            if (handler != null)
            {
                OnEventRemoved(this, eventName);
            }
        }
        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public string GetEventKey(string eventName)
        {
            return eventName;
        }

        public string GetEventKey<T>() where T : IntegrationMQEvent
        {
            return typeof(T).FullName;
        }
    }
}
