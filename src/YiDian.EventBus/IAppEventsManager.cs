using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        void RegisterEvents(string name, string version, List<Type> types);
    }
}
