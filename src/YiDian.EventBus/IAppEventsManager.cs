
using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        void RegisterEvent<T>(string appName, string version) where T : IntegrationMQEvent;
        CheckResult VaildityTest(string appName, string version);
        string GetVersion(string appName);
        AppMetas ListEvents(string appName);
        string GetEventId<T>(string appName) where T : IntegrationMQEvent;
        List<EventId> GetEventIds(string appname);
    }
    public class CheckResult
    {
        public bool IsVaild { get; set; }
        public string InvaildMessage { get; set; }
    }
    public enum AttrType
    {
        None = 0,
        Index = 1
    }
    public class PropertyMetaInfo
    {
        public static readonly string P_Double = "double";
        public static readonly string P_Int32 = "int32";
        public static readonly string P_Int64 = "int64";
        public static readonly string P_UInt32 = "uint32";
        public static readonly string P_UInt64 = "uint64";
        public static readonly string P_String = "string";
        public static readonly string P_Boolean = "boolean";
        public static string[] MetaTypeValues = new string[] { P_Int32, P_Int64, P_UInt32, P_UInt64, P_String, P_Boolean, P_Double };
        public PropertyMetaInfo()
        {
            Attr = MetaAttr.None;
        }
        public string Name { get; set; }
        public string Type { get; set; }
        public MetaAttr Attr { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }
    public class ClassMeta
    {
        public ClassMeta()
        {
            Properties = new List<PropertyMetaInfo>();
            Attr = MetaAttr.None;
        }
        public MetaAttr Attr { get; set; }
        public string Name { get; set; }
        public List<PropertyMetaInfo> Properties { get; set; }
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Attr\":");
            Attr.ToJson(sb);
            sb.Append('[');
            foreach (var p in Properties)
            {
                p.ToJson(sb);
            }
            sb.Append("]}");
        }
    }
    public class MetaAttr
    {
        public static MetaAttr None;
        static MetaAttr()
        {
            None = new MetaAttr() { AttrType = AttrType.None, Value = string.Empty };
        }
        public AttrType AttrType { get; set; }
        public string Value { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }
    public class AppMetas
    {
        public AppMetas()
        {
            MetaInfos = new List<ClassMeta>();
        }
        public List<ClassMeta> MetaInfos { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
    }
    public class EventId
    {
        public string Name { get; set; }
        public string ID { get; set; }
    }
}