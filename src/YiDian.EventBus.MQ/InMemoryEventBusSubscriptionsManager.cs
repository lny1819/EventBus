using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    public class InMemorySubFactory : IEventBusSubscriptionsManagerFactory
    {
        readonly IAppEventsManager _manager;
        readonly ILogger<IEventBusSubscriptionsManager> _logger;
        public InMemorySubFactory(IAppEventsManager manager, ILogger<IEventBusSubscriptionsManager> logger)
        {
            _logger = logger;
            _manager = manager;
        }
        readonly List<InMemoryEventBusSubscriptionsManager> __containers = new List<InMemoryEventBusSubscriptionsManager>();
        public IEventBusSubscriptionsManager GetOrCreateByQueue(string queueName)
        {
            var mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
            if (mgr != null) return mgr;
            lock (__containers)
            {
                mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
                if (mgr != null) return mgr;
                mgr = new InMemoryEventBusSubscriptionsManager(queueName, _manager, _logger);
                __containers.Add(mgr);
                return mgr;
            }
        }
    }
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        readonly List<string> __bytes_subs = new List<string>();
        readonly ConcurrentDictionary<string, List<SubscriptionInfo>> _handlers;
        readonly ILogger<IEventBusSubscriptionsManager> _logger;

        public event EventHandler<string> OnEventRemoved;
        public event EventHandler<string> OnEventAdd;
        SubscriptionInfo _bytesHandler = null;
        readonly IAppEventsManager _manager;
        public InMemoryEventBusSubscriptionsManager(string name, IAppEventsManager manager, ILogger<IEventBusSubscriptionsManager> logger)
        {
            _logger = logger;
            _manager = manager;
            QueueName = name;
            _handlers = new ConcurrentDictionary<string, List<SubscriptionInfo>>(StringComparer.OrdinalIgnoreCase);
        }
        public string QueueName { get; }
        public void AddSubscription<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            eventName = GetEventKey(eventName);
            if (__bytes_subs.Contains(eventName)) return;
            lock (__bytes_subs)
            {
                if (__bytes_subs.Contains(eventName)) return;
                __bytes_subs.Add(eventName);
                _bytesHandler = SubscriptionInfo.Dynamic(typeof(TH), null);
                RaiseOnEventAdd(eventName);
            }
        }
        public void AddSubscription<T, TH>()
            where T : IntegrationMQEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
            var handler = FastInvoke.GetMethodInvoker(method);
            DoAddSubscription(typeof(TH), typeof(T), handler, eventName, false);
        }
        public void AddSubscription<T, TH>(string eventName)
          where T : IntegrationMQEvent
          where TH : IIntegrationEventHandler<T>
        {
            eventName = GetEventKey(eventName);
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
                        RaiseOnEventAdd(eventName);
                    }
                }
            }
            if (isDynamic) items.Add(SubscriptionInfo.Dynamic(handlerType, handler));
            else items.Add(SubscriptionInfo.Typed(handlerType, eventType, handler));
        }

        public void RemoveSubscription<TH>(string eventName)
            where TH : IDynamicBytesHandler
        {
            eventName = GetEventKey(eventName);
            if (!__bytes_subs.Contains(eventName)) return;
            lock (__bytes_subs)
            {
                if (__bytes_subs.Remove(eventName))
                    RaiseOnEventRemoved(eventName);
                if (__bytes_subs.Count == 0) _bytesHandler = null;
            }
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
            eventName = GetEventKey(eventName);
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
            bool sub_dynamic = false;
            eventName = GetEventKey(eventName);
            foreach (var byte_key in __bytes_subs)
            {
                if (byte_key.StartsWith("#."))
                {
                    ReadOnlySpan<char> span = byte_key;
                    ReadOnlySpan<char> key = eventName;
                    sub_dynamic = key.Contains(span.Slice(2), StringComparison.OrdinalIgnoreCase);
                }
                else if (byte_key.EndsWith(".#"))
                {
                    ReadOnlySpan<char> span = byte_key;
                    ReadOnlySpan<char> key = eventName;
                    var i = span.Slice(2).CompareTo(eventName, StringComparison.OrdinalIgnoreCase);
                    sub_dynamic = key.Contains(span.Slice(0, byte_key.Length - 2), StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    ReadOnlySpan<char> span = byte_key;
                    ReadOnlySpan<char> key = eventName;
                    sub_dynamic = key.Contains(span, StringComparison.OrdinalIgnoreCase);
                }
                if (sub_dynamic) break;
            }
            var flag = _handlers.TryGetValue(eventName, out List<SubscriptionInfo> res);
            if (!flag && !sub_dynamic) return null;
            if (sub_dynamic)
            {
                if (!flag) res = new List<SubscriptionInfo>();
                res.Add(_bytesHandler);
            }
            return res;
        }
        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            if (handler != null)
            {
                OnEventRemoved(this, eventName);
            }
        }
        private void RaiseOnEventAdd(string eventName)
        {
            var handler = OnEventAdd;
            if (handler != null)
            {
                OnEventAdd(this, eventName);
            }
        }

        //void SubEvent(string eventName)
        //{
        //    if (__subs.Contains(eventName)) return;
        //    lock (__subs)
        //    {
        //        if (!__subs.Contains(eventName))
        //        {
        //            __subs.Add(eventName);
        //            RaiseOnEventAdd(eventName);
        //        }
        //    }
        //}
        //void UnSubEvent(string eventName)
        //{
        //    if (__subs.Contains(eventName))
        //    {
        //        lock (__subs)
        //        {
        //            if (__subs.Contains(eventName))
        //            {
        //                __subs.Remove(eventName);
        //                RaiseOnEventRemoved(eventName);
        //            }
        //        }
        //    }
        //}

        public string GetEventKey(string eventName)
        {
            return eventName;
        }
        public string GetEventKey<T>() where T : IntegrationMQEvent
        {
            var res = _manager.GetEventId<T>();
            if (!res.IsVaild)
                _logger.LogError("when get event key, response error: " + res.InvaildMessage);
            return res.InvaildMessage;
        }
    }
}
