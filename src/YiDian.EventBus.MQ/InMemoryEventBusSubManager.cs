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
        public InMemorySubFactory(IAppEventsManager manager)
        {
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
                mgr = new InMemoryEventBusSubManager(queueName, _manager);
                __containers.Add(mgr);
                return mgr;
            }
        }
    }
    public class InMemoryEventBusSubManager : IEventBusSubManager
    {
        readonly List<SubscriptionInfo> _subInfos;
        readonly HashSet<string> _sub_keys;
        readonly IAppEventsManager _manager;

        public event EventHandler<ValueTuple<string, string>> OnEventRemoved;
        public event EventHandler<ValueTuple<string, string>> OnEventAdd;

        readonly ConcurrentDictionary<string, string> dic;
        public InMemoryEventBusSubManager(string name, IAppEventsManager manager)
        {
            dic = new ConcurrentDictionary<string, string>();
            _manager = manager;
            _subInfos = new List<SubscriptionInfo>();
            _sub_keys = new HashSet<string>();
            QueueName = name;
        }
        public string QueueName { get; }
        public void AddBytesSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IBytesHandler
        {
            SubMessage(subkey, brokerName);
            lock (_subInfos)
            {
                var count = _subInfos.Where(x => x.IsDynamic && x.SubKey == subkey && x.HandlerType == typeof(TH)).Count();
                if (count != 0) return;
                var eventkey = GetEventKey<T>();
                var flag = eventkey == subkey;
                var enventkey = GetEventKey<T>();
                var info = SubscriptionInfo.Dynamic(subkey, enventkey, flag, typeof(TH), null, brokerName);
                _subInfos.Add(info);
            }
        }
        public void AddSubscription<T, TH>(string subkey, string brokerName)
          where T : IMQEvent
          where TH : IEventHandler<T>
        {
            SubMessage(subkey, brokerName);
            lock (_subInfos)
            {
                var count = _subInfos.Where(x => !x.IsDynamic && x.SubKey == subkey && x.HandlerType == typeof(TH)).Count();
                if (count != 0) return;
                var eventkey = GetEventKey<T>();
                var flag = eventkey == subkey;
                var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
                var handler = FastInvoke.GetMethodInvoker(method);
                var info = SubscriptionInfo.Typed(subkey, eventkey, flag, typeof(TH), typeof(T), handler, brokerName);
                _subInfos.Add(info);
            }
        }

        public void RemoveSubscription(string subkey, string brokerName)
        {
            UnSubMessage(subkey, brokerName);
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
                if (i == 0) UnSubMessage(one.SubKey, one.BrokerName);
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
                if (i == 0) UnSubMessage(one.SubKey, one.BrokerName);
            }
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            return _subInfos.Where(x => string.Compare(x.EventKey, eventName, true) == 0);
        }
        private void SubMessage(string subkey, string brokerName)
        {
            lock (_sub_keys)
            {
                if (!_sub_keys.Contains(subkey))
                {
                    _sub_keys.Add(subkey);
                    RaiseOnEventAdd(subkey, brokerName);
                }
            }
        }
        private void UnSubMessage(string subkey, string brokerName)
        {
            lock (_sub_keys)
            {
                if (_sub_keys.Contains(subkey))
                {
                    _sub_keys.Remove(subkey);
                }
            }
            RaiseOnEventRemoved(subkey, brokerName);
        }
        private void RaiseOnEventRemoved(string eventName, string brokerName)
        {
            var handler = OnEventRemoved;
            if (handler != null)
            {
                OnEventRemoved(this, (eventName, brokerName));
            }
        }
        private void RaiseOnEventAdd(string eventName, string brokerName)
        {
            var handler = OnEventAdd;
            if (handler != null)
            {
                OnEventAdd(this, (eventName, brokerName));
            }
        }
        public string GetEventKey<T>() where T : IMQEvent
        {
            return GetEventKey(typeof(T));
        }
        public string GetEventKey(Type type)
        {
            var typename = type.Name;
            if (dic.TryGetValue(typename, out string id)) return id;
            lock (dic)
            {
                if (dic.TryGetValue(typename, out id)) return id;
                var res = _manager.GetEventId(typename);
                if (!res.IsVaild && _manager.AllowNoRegisterEvent) return typename;
                if (!res.IsVaild && !_manager.AllowNoRegisterEvent) throw new Exception("when get event key, response error: " + res.InvaildMessage);
                dic.TryAdd(typename, res.InvaildMessage);
                return res.InvaildMessage;
            }
        }
    }
}
