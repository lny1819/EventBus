using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        CheckResult RegisterEvent<T>(string appName, string version) where T : IMQEvent;
        CheckResult VaildityTest(string appName, string version);
        CheckResult GetVersion(string appName);
        AppMetas ListEvents(string appName);
        CheckResult GetEventId(string typename);
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
        public static readonly string P_Date = "DateTime";
        public static readonly string P_Boolean = "Boolean";
        public static string[] MetaTypeValues = new string[] { P_Int32, P_Int64, P_UInt32, P_UInt64, P_String, P_Boolean, P_Double, P_Date };

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
    public class EnumMeta
    {
        public EnumMeta()
        {
            Values = new List<(string, int)>();
        }
        public string Name { get; set; }
        public List<ValueTuple<string, int>> Values { get; set; }
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Values\":[");
            for (var i = 0; i < Values.Count; i++)
            {
                sb.Append("{\"Item1\":\"");
                sb.Append(Values[i].Item1);
                sb.Append("\",\"Item2\":");
                sb.Append(Values[i].Item2.ToString());
                sb.Append("}");
                if (i != Values.Count - 1) sb.Append(',');
            }
            sb.Append("]}");
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
        public bool IsEventType { get; set; }
        public List<PropertyMetaInfo> Properties { get; set; }
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"IsEventType\":");
            sb.Append(IsEventType.ToString().ToLower());
            sb.Append(",\"Attr\":");
            if (Attr == null) sb.Append("null");
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
            Enums = new List<EnumMeta>();
        }
        public List<EnumMeta> Enums { get; set; }
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
            sb.Append("],\"Enums\":[");
            for (var i = 0; i < Enums.Count; i++)
            {
                Enums[i].ToJson(sb);
                if (i != Enums.Count - 1) sb.Append(',');
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