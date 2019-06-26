using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    public class InMemorySubFactory : IEventBusSubManagerFactory
    {
        readonly IAppEventsManager _manager;
        readonly ILogger<IEventBusSubManager> _logger;
        public InMemorySubFactory(IAppEventsManager manager, ILogger<IEventBusSubManager> logger)
        {
            _logger = logger;
            _manager = manager;
        }
        readonly List<InMemoryEventBusSubManager> __containers = new List<InMemoryEventBusSubManager>();
        public IEventBusSubManager GetOrCreateByQueue(string queueName)
        {
            var mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
            if (mgr != null) return mgr;
            lock (__containers)
            {
                mgr = __containers.FirstOrDefault(x => x.QueueName == queueName);
                if (mgr != null) return mgr;
                mgr = new InMemoryEventBusSubManager(queueName, _manager, _logger);
                __containers.Add(mgr);
                return mgr;
            }
        }
    }
    public class InMemoryEventBusSubManager : IEventBusSubManager
    {
        readonly List<SubscriptionInfo> _subInfos;
        readonly HashSet<string> _sub_keys;
        readonly ILogger<IEventBusSubManager> _logger;
        readonly IAppEventsManager _manager;

        public event EventHandler<string> OnEventRemoved;
        public event EventHandler<string> OnEventAdd;

        public InMemoryEventBusSubManager(string name, IAppEventsManager manager, ILogger<IEventBusSubManager> logger)
        {
            _logger = logger;
            _manager = manager;
            _subInfos = new List<SubscriptionInfo>();
            _sub_keys = new HashSet<string>();
            QueueName = name;
        }
        public string QueueName { get; }
        public void AddBytesSubscription<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            SubMessage(subkey);
            lock (_subInfos)
            {
                var count = _subInfos.Where(x => x.IsDynamic && x.SubKey == subkey && x.HandlerType == typeof(TH)).Count();
                if (count != 0) return;
                var eventkey = GetEventKey<T>();
                var flag = eventkey == subkey;
                var enventkey = GetEventKey<T>();
                var info = SubscriptionInfo.Dynamic(subkey, enventkey, flag, typeof(TH), null);
                _subInfos.Add(info);
            }
        }
        public void AddSubscription<T, TH>(string subkey)
          where T : IMQEvent
          where TH : IEventHandler<T>
        {
            SubMessage(subkey);
            lock (_subInfos)
            {
                var count = _subInfos.Where(x => !x.IsDynamic && x.SubKey == subkey && x.HandlerType == typeof(TH)).Count();
                if (count != 0) return;
                var eventkey = GetEventKey<T>();
                var flag = eventkey == subkey;
                var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
                var handler = FastInvoke.GetMethodInvoker(method);
                var info = SubscriptionInfo.Typed(subkey, eventkey, flag, typeof(TH), typeof(T), handler);
                _subInfos.Add(info);
            }
        }

        public void RemoveSubscription(string subkey)
        {
            UnSubMessage(subkey);
            lock (_subInfos)
            {
                var finds = _subInfos.Where(x => x.SubKey == subkey).ToList();
                finds.ForEach(x => _subInfos.Remove(x));
            }
        }
        public void RemoveBytesSubscription<T, TH>()
            where T : IMQEvent
            where TH : IBytesHandler
        {
            lock (_subInfos)
            {
                var one = _subInfos.FirstOrDefault(x => x.EventType == typeof(T) && x.HandlerType == typeof(TH));
                if (one == null || !one.CanRemoveSubByEvent) return;
                var i = _subInfos.Where(x => x.EventType == typeof(T) && x.IsDynamic).Count();
                _subInfos.Remove(one);
                if (i == 0) UnSubMessage(one.SubKey);
            }
        }
        public void RemoveSubscription<T, TH>()
            where TH : IEventHandler<T>
            where T : IMQEvent
        {
            lock (_subInfos)
            {
                var one = _subInfos.FirstOrDefault(x => x.EventType == typeof(T) && x.HandlerType == typeof(TH));
                if (one == null || !one.CanRemoveSubByEvent) return;
                var i = _subInfos.Where(x => x.EventType == typeof(T) && !x.IsDynamic).Count();
                _subInfos.Remove(one);
                if (i == 0) UnSubMessage(one.SubKey);
            }
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            return _subInfos.Where(x => x.EventKey == eventName);
        }
        private void SubMessage(string subkey)
        {
            lock (_sub_keys)
            {
                if (!_sub_keys.Contains(subkey))
                {
                    _sub_keys.Add(subkey);
                    RaiseOnEventAdd(subkey);
                }
            }
        }
        private void UnSubMessage(string subkey)
        {
            lock (_sub_keys)
            {
                if (_sub_keys.Contains(subkey))
                {
                    _sub_keys.Remove(subkey);
                    RaiseOnEventRemoved(subkey);
                }
            }
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

        public string GetEventKey<T>() where T : IMQEvent
        {
            var res = _manager.GetEventId<T>();
            if (!res.IsVaild)
                _logger.LogError("when get event key, response error: " + res.InvaildMessage);
            return res.InvaildMessage;
        }
    }
}
