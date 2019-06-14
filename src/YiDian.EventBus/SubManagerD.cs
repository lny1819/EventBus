using System;

namespace YiDian.EventBus
{
    public class SubManagerD
    {
        protected Action<string> _removeSub;
        protected Action<string> _addSub;
        protected IEventBusSubscriptionsManager _submgr;
        public SubManagerD()
        {

        }
        public SubManagerD(Action<string> removeSub, Action<string> addSub)
        {
            _removeSub = removeSub;
            _addSub = addSub;
            _submgr = new InMemoryEventBusSubscriptionsManager();
            _submgr.OnEventRemoved += SubManager_OnEventRemoved;
        }
        private void SubManager_OnEventRemoved(object sender, string eventName)
        {
            var subkey = GetSubKey(eventName);
            _removeSub(subkey);
        }
        protected virtual string GetSubKey(string eventName)
        {
            return eventName;
        }
        /// <summary>
        /// MQ消息订阅方法
        /// </summary>
        /// <param name="eventname">MQ订阅路由键</param>
        /// <param name="typename">订阅消息处理类型，用于判断是否重复订阅消息</param>
        public virtual void Sub(string eventname, string typename)
        {
            _addSub(eventname);
        }
        public IEventBusSubscriptionsManager SubManager { get { return _submgr; } }
    }
}
