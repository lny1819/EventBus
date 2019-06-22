
using System.Collections.Generic;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        void RegisterEvents(AppMetas metas);
        void RegisterEvent<T>(string appName, string version);
        CheckResult VaildityTest(string appName, string version);
        AppMetas GetAppEventTypes(string appName, string version);
    }
    public class CheckResult
    {
        public bool IsVaild { get; set; }
        public string InVaildMessage { get; set; }
    }
    public enum AttrType
    {
        Index = 0
    }
    public class PropertyMetaInfo
    {
        public static string[] MetaTypeValues = new string[] { "int32", "int64", "uint32", "uint64", "string", "boolean" };

        public string Name { get; set; }
        public string Type { get; set; }
        public MetaAttr Attr { get; set; }
    }
    public class ClassMeta
    {
        public string Name { get; set; }
        public List<PropertyMetaInfo> Properties { get; set; }
    }
    public struct MetaAttr
    {
        public AttrType AttrType { get; set; }
        public string Value { get; set; }
    }
    public class AppMetas
    {
        public List<ClassMeta> MetaInfos { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
    }
}