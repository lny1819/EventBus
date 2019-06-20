using System;
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        void RegisterEvents(string name, string version, List<MetaInfo> types);
        AppMetas GetAppEventTypes(string app);
    }
    public class MetaInfo
    {

    }
    public class AppMetas
    {
        public List<MetaInfo> MetaInfos { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
    }
}