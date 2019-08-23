using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class DefaultEventsManager : IAppEventsManager
    {
        static readonly CheckResult Success = new CheckResult() { IsVaild = true, InvaildMessage = "" };

        public bool AllowNoRegisterEvent => true;

        public CheckResult GetEventId(string typename)
        {
            return new CheckResult() { IsVaild = true, InvaildMessage = typename };
        }

        public List<EventId> GetEventIds(string appname)
        {
            return new List<EventId>();
        }

        public CheckResult GetVersion(string appName)
        {
            return new CheckResult() { IsVaild = true, InvaildMessage = "0.0" };
        }

        public AppMetas ListEvents(string appName)
        {
            return new AppMetas() { Enums = new List<EnumMeta>(), MetaInfos = new List<ClassMeta>(), Name = appName, Version = "0.0" };
        }

        public CheckResult RegisterEvent<T>(string appName, string version, bool enableDefaultSeralize) where T : IMQEvent
        {
            return Success;
        }

        public CheckResult VaildityTest(string appName, string version)
        {
            return Success;
        }
    }
}
