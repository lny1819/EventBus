using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public class SubManagerK : SubManagerD
    {
        Dictionary<string, string> eventNames;
        public SubManagerK(Action<string> removeSub, Action<string> addSub) : base(removeSub, addSub)
        {
            eventNames = new Dictionary<string, string>();
        }

        protected override string GetSubKey(string eventName)
        {
            return eventNames[eventName];
        }

        public override void Sub(string keyname, string typename)
        {
            keyname = keyname.ToLower();
            typename = typename.ToLower();
            if (eventNames.ContainsKey(typename) && eventNames[typename] != keyname)
            {
                throw new ArgumentException($"EventType {typename} already registered for subKey '{keyname}'", nameof(typename));
            }
            eventNames[typename] = keyname;
            base.Sub(keyname, typename);
        }
    }
}
