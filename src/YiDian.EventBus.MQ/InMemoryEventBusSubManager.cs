using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    internal class InMemorySubFactory : IEventBusSubManagerFactory
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
    internal class InMemoryEventBusSubManager : IEventBusSubManager
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
        /// <summary>
        /// 通过制定Key和Exchange 订阅消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <typeparam name="TH">消息消费类型</typeparam>
        /// <param name="subkey">消息路由键</param>
        /// <param name="brokerName">交换机名称</param>
        public void AddSubscription<T, TH>(string subkey, string brokerName)
          where T : IMQEvent
          where TH : IEventHandler<T>
        {
            lock (_subInfos)
            {
                var eventkey = GetEventKey<T>();
                var flag = _subInfos.Exists(x => !x.IsDynamic && x.SubKey == subkey && x.EventKey == eventkey && x.HandlerType == typeof(TH) && x.EventType == typeof(T) && x.BrokerName == brokerName);
                if (!flag)
                {
                    var method = typeof(TH).GetMethod("Handle", new Type[] { typeof(T) });
                    var handler = FastInvoke.GetMethodInvoker(method);
                    var info = SubscriptionInfo.Typed(subkey, eventkey, typeof(TH), typeof(T), handler, brokerName);
                    _subInfos.Add(info);
                }
            }
            SubMessage(subkey, brokerName);
        }
        public void RemoveSubscription<T, TH>(string subkey, string brokerName)
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            lock (_subInfos)
            {
                var eventkey = GetEventKey<T>();
                var one = _subInfos.FirstOrDefault(x => !x.IsDynamic && x.SubKey == subkey && x.EventKey == eventkey && x.HandlerType == typeof(TH) && x.EventType == typeof(T) && x.BrokerName == brokerName);
                if (one == null) return;
                var i = _subInfos.Where(x => x.SubKey == subkey && x.BrokerName == brokerName).Count();
                if (i == 1) UnSubMessage(one.SubKey, one.BrokerName);
                _subInfos.Remove(one);
            }
        }
        public void AddBytesSubscription<TH>(string subkey, string brokerName) where TH : IBytesHandler
        {
            lock (_subInfos)
            {
                var flag = _subInfos.Exists(x => x.IsDynamic && x.SubKey == subkey && x.EventKey == "" && x.HandlerType == typeof(TH) && x.BrokerName == brokerName);
                if (!flag)
                {
                    var info = SubscriptionInfo.Dynamic(subkey, "", typeof(TH), null, brokerName);
                    _subInfos.Add(info);
                }
            }
            SubMessage(subkey, brokerName);
        }

        public void RemoveBytesSubscription<TH>(string subkey, string brokerName) where TH : IBytesHandler
        {
            lock (_subInfos)
            {
                var one = _subInfos.FirstOrDefault(x => x.IsDynamic && x.SubKey == subkey && x.EventKey == "" && x.HandlerType == typeof(TH) && x.BrokerName == brokerName);
                if (one == null) return;
                var i = _subInfos.Where(x => x.SubKey == subkey && x.BrokerName == brokerName).Count();
                if (i == 1) UnSubMessage(one.SubKey, one.BrokerName);
                _subInfos.Remove(one);
            }
        }
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName, string brokerName)
        {
            return _subInfos.Where(x => !x.IsDynamic && string.Compare(x.EventKey, eventName, true) == 0 && string.Compare(x.BrokerName, brokerName, true) == 0);
        }
        public IEnumerable<SubscriptionInfo> GetDymaicHandlersBySubKey(string key, string brokerName, bool match)
        {
            if (!match)
            {
                if (key == "*") return _subInfos.Where(x => x.IsDynamic && string.Compare(x.BrokerName, brokerName, true) == 0);
                return _subInfos.Where(x => x.IsDynamic && string.Compare(x.SubKey, key, true) == 0 && string.Compare(x.BrokerName, brokerName, true) == 0);
            }
            return _subInfos.Where(x => x.IsDynamic && MathKey(x.SubKey, key) && string.Compare(x.BrokerName, brokerName, true) == 0);
        }
        private bool MathKey(string subKey, string key)
        {
            if (string.Compare(subKey, key, true) == 0) return true;
            if (subKey == "#") return true;
            var arr1 = subKey.Split('.');
            var arr2 = key.Split('.');
            var j = 0;
            for (var i = 0; i < arr1.Length; i++)
            {
                var s = arr1[i];
                if (s == "*")
                {
                    j++;
                    continue;
                }
                else if (s == "#")
                {
                    if (i == arr1.Length - 1) return true;
                    i += 1;
                    s = arr1[i];
                    for (; j < arr2.Length; j++)
                    {
                        if (s == arr2[j])
                        {
                            goto Next;
                        }
                    }
                    return false;
                }
                if (s != arr2[j]) return false;
                Next:
                j++;
                continue;
            }
            if (j != arr2.Length) return false;
            return true;
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
