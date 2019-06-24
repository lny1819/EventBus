
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
        public static readonly string P_Int32 = "Int32";
        public static readonly string P_Int64 = "Int64";
        public static readonly string P_UInt32 = "UInt32";
        public static readonly string P_UInt64 = "UInt64";
        public static readonly string P_String = "string";
        public static readonly string P_Boolean = "Boolean";
        public static string[] MetaTypeValues = new string[] { P_Int32, P_Int64, P_UInt32, P_UInt64, P_String, P_Boolean, P_Double };

        public string Name { get; set; }
        public string Type { get; set; }
        public MetaAttr Attr { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Type\":\"");
            sb.Append(Type);
            sb.Append("\",\"Attr\":");
            if (Attr == null) sb.Append("null");
            else Attr.ToJson(sb);
            sb.Append("}");
        }
    }
    public class ClassMeta
    {
        public ClassMeta()
        {
            Properties = new List<PropertyMetaInfo>();
        }
        public MetaAttr Attr { get; set; }
        public string Name { get; set; }
        public List<PropertyMetaInfo> Properties { get; set; }
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Attr\":");
            if(Attr==null) sb.Append("null");
            else Attr.ToJson(sb);
            sb.Append(",\"Properties\":[");
            for (var i = 0; i < Properties.Count; i++)
            {
                Properties[i].ToJson(sb);
                if (i != Properties.Count - 1) sb.Append(',');
            }
            sb.Append("]}");
        }
    }
    public class MetaAttr
    {
        public AttrType AttrType { get; set; }
        public string Value { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            sb.Append('{');
            sb.Append("\"AttrType\":");
            sb.Append(((int)AttrType).ToString());
            sb.Append(',');
            sb.Append("\"Value\":\"");
            sb.Append(Value);
            sb.Append("\"}");
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
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Version\":\"");
            sb.Append(Version);
            sb.Append("\",\"MetaInfos\":[");
            for (var i = 0; i < MetaInfos.Count; i++)
            {
                MetaInfos[i].ToJson(sb);
                if (i != MetaInfos.Count - 1) sb.Append(',');
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
    public class EventId
    {
        public string Name { get; set; }
        public string ID { get; set; }
    }
}