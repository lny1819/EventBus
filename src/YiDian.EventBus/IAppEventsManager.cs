using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息名称管理器，注册消息，生成或消息编码
    /// </summary>
    public interface IAppEventsManager
    {
        /// <summary>
        /// 是否允许使用不经过注册的消息类型
        /// </summary>
        bool AllowNoRegisterEvent { get; }
        /// <summary>
        /// 注册一个消息类型
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="appName">APP名称</param>
        /// <param name="version">APP版本</param>
        /// <param name="enableDefaultSeralize">是否是默认序列化</param>
        /// <returns></returns>
        CheckResult RegisterEvent<T>(string appName, string version, bool enableDefaultSeralize = true) where T : IMQEvent;
        /// <summary>
        /// 指示指定版本和名称的APP是否存在
        /// </summary>
        /// <param name="appName">APP名称</param>
        /// <param name="version">APP版本</param>
        /// <returns></returns>
        CheckResult VaildityTest(string appName, string version);
        /// <summary>
        /// 获取APP最新版本号
        /// </summary>
        /// <param name="appName">APP名称</param>
        /// <returns></returns>
        CheckResult GetVersion(string appName);
        /// <summary>
        /// 获取APP最新的所有消息详情
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        AppMetas ListEvents(string appName);
        /// <summary>
        /// 获取指定类型名称的消息编号
        /// </summary>
        /// <param name="typename"></param>
        /// <returns></returns>
        CheckResult GetEventId(string typename);
        /// <summary>
        /// 获取APP最新的所有消息编码
        /// </summary>
        /// <param name="appname">APP名称</param>
        /// <returns></returns>
        List<EventId> GetEventIds(string appname);

    }
    /// <summary>
    /// 获取结果
    /// </summary>
    public class CheckResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsVaild { get; set; }
        /// <summary>
        /// 提示信息
        /// </summary>
        public string InvaildMessage { get; set; }
    }
    /// <summary>
    /// 消息字段特性枚举
    /// </summary>
    public enum AttrType
    {
        /// <summary>
        /// 没有使用特性
        /// </summary>
        None = 0,
        /// <summary>
        /// 使用l了<see cref="KeyIndex"/>特性
        /// </summary>
        Index = 1
    }
    /// <summary>
    /// 消息字段描述信息
    /// </summary>
    public class PropertyMetaInfo
    {
        /// <summary>
        /// 64位浮点数
        /// </summary>
        public const string P_Double = "double";
        /// <summary>
        /// 16位有符号整数
        /// </summary>
        public const string P_Int16 = "Int16";
        /// <summary>
        /// 16位无符号整数
        /// </summary>
        public const string P_UInt16 = "UInt16";
        /// <summary>
        /// 32位有符号整数
        /// </summary>
        public const string P_Int32 = "Int32";
        /// <summary>
        /// 32位无符号整数
        /// </summary>
        public const string P_UInt32 = "UInt32";
        /// <summary>
        /// 64位无符号整数
        /// </summary>
        public const string P_UInt64 = "UInt64";
        /// <summary>
        /// 64位有符号整数
        /// </summary>
        public const string P_Int64 = "Int64";
        /// <summary>
        /// 字符串
        /// </summary>
        public const string P_String = "string";
        /// <summary>
        /// 日期
        /// </summary>
        public const string P_Date = "DateTime";
        /// <summary>
        /// 单字节
        /// </summary>
        public const string P_Byte = "Byte";
        /// <summary>
        /// 布尔类型
        /// </summary>
        public const string P_Boolean = "Boolean";
        /// <summary>
        /// 枚举类型
        /// </summary>
        public const string P_Enum = "Enum";
        /// <summary>
        /// 数组类型
        /// </summary>
        public const string P_Array = "Array";
        /// <summary>
        /// 基础数据类型列表
        /// </summary>
        public static string[] MetaTypeValues = new string[] { P_Int32, P_Int64, P_UInt32, P_UInt64, P_String, P_Boolean, P_Double, P_Date, P_Byte };
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 字段数据类型
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 序列化编号
        /// </summary>
        public int SeralizeIndex { get; set; }
        /// <summary>
        /// 字段特性
        /// </summary>
        public MetaAttr Attr { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Type\":\"");
            sb.Append(Type);
            sb.Append("\",\"SeralizeIndex\":");
            sb.Append(SeralizeIndex);
            sb.Append(",\"Attr\":");
            if (Attr == null) sb.Append("null");
            else Attr.ToJson(sb);
            sb.Append("}");
        }
    }
    /// <summary>
    ///  枚举值描述信息
    /// </summary>
    public class EnumValue
    {
        /// <summary>
        /// 枚举值名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 枚举值
        /// </summary>
        public int Value { get; set; }
    }
    /// <summary>
    /// 枚举元数据
    /// </summary>
    public class EnumMeta
    {
        /// <summary>
        /// 枚举元数据
        /// </summary>
        public EnumMeta()
        {
            Values = new List<EnumValue>();
        }
        /// <summary>
        /// 枚举名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 是否序列化 暂不使用
        /// </summary>
        public bool DefaultSeralize { get; set; }
        /// <summary>
        /// 枚举值
        /// </summary>
        public List<EnumValue> Values { get; set; }
        /// <summary>
        /// 生成对象的JSON格式字符串
        /// </summary>
        /// <param name="sb"></param>
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"DefaultSeralize\":");
            sb.Append(DefaultSeralize.ToString().ToLower());
            sb.Append(",\"Values\":[");
            for (var i = 0; i < Values.Count; i++)
            {
                sb.Append("{\"Name\":\"");
                sb.Append(Values[i].Name);
                sb.Append("\",\"Value\":");
                sb.Append(Values[i].Value.ToString());
                sb.Append("}");
                if (i != Values.Count - 1) sb.Append(',');
            }
            sb.Append("]}");
        }
    }
    /// <summary>
    /// 消息类型元数据
    /// </summary>
    public class ClassMeta
    {
        /// <summary>
        /// 消息类型元数据
        /// </summary>
        public ClassMeta()
        {
            Properties = new List<PropertyMetaInfo>();
        }
        /// <summary>
        /// 类型特性
        /// </summary>
        public MetaAttr Attr { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 是否是消息类型
        /// </summary>
        public bool IsEventType { get; set; }
        /// <summary>
        /// 是否使用默认序列化
        /// </summary>
        public bool DefaultSeralize { get; set; }
        /// <summary>
        /// 类型字段元数据
        /// </summary>
        public List<PropertyMetaInfo> Properties { get; set; }
        /// <summary>
        /// 生成对象的JSON格式字符串
        /// </summary>
        /// <param name="sb"></param>
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"IsEventType\":");
            sb.Append(IsEventType.ToString().ToLower());
            sb.Append(",\"DefaultSeralize\":");
            sb.Append(DefaultSeralize.ToString().ToLower());
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
    /// <summary>
    /// 特性元数据
    /// </summary>
    public class MetaAttr
    {
        /// <summary>
        /// 特性类型
        /// </summary>
        public AttrType AttrType { get; set; }
        /// <summary>
        /// 特性值
        /// </summary>
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
    /// <summary>
    /// APP元数据
    /// </summary>
    public class AppMetas
    {
        /// <summary>
        /// APP元数据
        /// </summary>
        public AppMetas()
        {
            MetaInfos = new List<ClassMeta>();
            Enums = new List<EnumMeta>();
        }
        /// <summary>
        /// 枚举字段元数据
        /// </summary>
        public List<EnumMeta> Enums { get; set; }
        /// <summary>
        /// 类型元数据
        /// </summary>
        public List<ClassMeta> MetaInfos { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// APP名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 生成对象的JSON格式字符串
        /// </summary>
        /// <returns></returns>
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
    /// <summary>
    /// 消息编码
    /// </summary>
    public class EventId
    {
        /// <summary>
        /// 消息名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 消息编码
        /// </summary>
        public string ID { get; set; }
    }
}