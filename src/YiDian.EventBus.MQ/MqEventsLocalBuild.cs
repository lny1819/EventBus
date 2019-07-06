using YiDian.Soa.Sp;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace YiDian.EventBus.MQ
{

    static class FileWrite
    {
        public static void WriteLine(this FileStream fs, string value)
        {
            fs.Write(Encoding.UTF8.GetBytes(value));
            fs.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        }
        public static void Write(this FileStream fs, string value)
        {
            fs.Write(Encoding.UTF8.GetBytes(value));
        }
    }

    public class MqEventsLocalBuild : IAppRun
    {
        const string s_property = "        public {0} {1} ";
        const string list_property = "        public {0}[] {1} ";
        const string attr_property = "        [KeyIndex({0})]";
        const string index_property = "        [SeralizeIndex({0})]";
        ILogger<MqEventsLocalBuild> _logger;
        IAppEventsManager _eventsManager;
        public string Name { get; private set; }
        public void Run(ISoaServiceHost host, string name, string[] args)
        {
            var scope = host.ServicesProvider.CreateScope();
            //--loadevents -app history,userapi -path /data/his
            _eventsManager = scope.ServiceProvider.GetService<IAppEventsManager>();
            _logger = scope.ServiceProvider.GetService<ILogger<MqEventsLocalBuild>>();
            Name = name;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--loadevents")
                {
                    string appname = "";
                    string path = "";
                    var n1 = args[i + 1];
                    var n2 = args[i + 3];
                    var v1 = args[i + 2];
                    var v2 = args[i + 4];
                    var arr1 = new string[] { n1, v1 };
                    var arr2 = new string[] { n2, v2 };
                    var arr = new string[2][] { arr1, arr2 };
                    foreach (var n in arr)
                    {
                        if (n[0] == "-app") appname = n[1];
                        else if (n[0] == "-path") path = n[1];
                    }
                    var sp = host.ServicesProvider;
                    var appnames = appname.Split(',');
                    foreach (var app in appnames)
                    {
                        var version = "";
                        var res = _eventsManager.GetVersion(app);
                        if (res.IsVaild)
                        {
                            version = res.InvaildMessage;
                            if (version == "0.0") continue;
                            res = _eventsManager.VaildityTest(app, version);
                        }
                        if (!res.IsVaild)
                        {
                            _logger.LogError("EventModel vaild-test failed with err-msg:" + res.InvaildMessage + ",app=" + app + ",version=" + version);
                            return;
                        }
                        var app_path = Path.Combine(path, "EventModels", app);
                        if (!Directory.Exists(app_path)) Directory.CreateDirectory(app_path);
                        var versionFile = Path.Combine(app_path, version + ".v");
                        if (File.Exists(versionFile)) continue;
                        var meta = _eventsManager.ListEvents(app);
                        LocalBuildEvents(meta, app_path);
                    }
                    break;
                }
            }
            scope.Dispose();
        }
        private void LocalBuildEvents(AppMetas meta, string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.cs"))
                File.Delete(file);
            CreateFiles(meta, path);
        }
        private void CreateFiles(AppMetas appmeta, string dir)
        {
            var s_namespace = appmeta.Name;
            Directory.CreateDirectory(dir);
            foreach (var meta in appmeta.MetaInfos)
            {
                CreateMainClassFile(dir, s_namespace, meta);
                if (meta.DefaultSeralize)
                    CreateSeralizeClassFile(dir, s_namespace, meta);
            }
            foreach (var meta in appmeta.Enums)
            {
                FileStream file = null;
                try
                {
                    file = File.Create(Path.Combine(dir, meta.Name + ".cs"));
                    file.WriteLine("using System;");
                    file.WriteLine("namespace EventModels." + s_namespace);
                    file.WriteLine("{");
                    file.WriteLine("    public enum " + meta.Name);
                    file.WriteLine("    {");
                    for (var i = 0; i < meta.Values.Count; i++)
                    {
                        if (i != meta.Values.Count - 1)
                        {
                            file.Write("        " + meta.Values[i].Item1 + " = " + meta.Values[i].Item2);
                            file.WriteLine(",");
                        }
                        else file.WriteLine("        " + meta.Values[i].Item1 + " = " + meta.Values[i].Item2);
                    }
                    file.WriteLine("    }");
                    file.WriteLine("}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                finally
                {
                    file?.Close();
                }
            }
            var versionFile = Path.Combine(dir, appmeta.Version + ".v");
            var v_file = File.OpenWrite(versionFile);
            var json = appmeta.ToJson();
            v_file.Write(json);
            v_file.Close();
        }
        void CreateMainClassFile(string dir, string s_namespace, ClassMeta meta)
        {
            FileStream file = null;
            try
            {
                file = File.Create(Path.Combine(dir, meta.Name + ".cs"));
                file.WriteLine("using System;");
                file.WriteLine("using System.Collections.Generic;");
                file.WriteLine("using YiDian.EventBus;");
                file.WriteLine("using YiDian.EventBus.MQ.KeyAttribute;");
                file.WriteLine("namespace EventModels." + s_namespace);
                file.WriteLine("{");
                if (meta.IsEventType)
                    file.WriteLine("    public partial class " + meta.Name + ": " + typeof(IMQEvent).Name);
                else
                    file.WriteLine("    public partial class " + meta.Name);
                file.WriteLine("    {");
                foreach (var p in meta.Properties)
                {
                    if (p.Attr != null)
                    {
                        if (p.Attr.AttrType == AttrType.Index)
                        {
                            file.WriteLine(string.Format(attr_property, p.Attr.Value));
                        }
                    }
                    file.WriteLine(string.Format(index_property, p.SeralizeIndex.ToString()));
                    if (p.Type.StartsWith(PropertyMetaInfo.P_Array))
                    {
                        var type = p.Type.Substring(6);
                        file.Write(string.Format(list_property, type, p.Name));
                    }
                    else if (p.Type.StartsWith(PropertyMetaInfo.P_Enum))
                    {
                        var type = p.Type.Substring(5);
                        file.Write(string.Format(s_property, type, p.Name));
                    }
                    else file.Write(string.Format(s_property, p.Type, p.Name));
                    file.WriteLine("{ get; set; }");
                }
                file.WriteLine("    }");
                file.WriteLine("}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            finally
            {
                file?.Close();
            }
        }
        void CreateSeralizeClassFile(string dir, string s_namespace, ClassMeta meta)
        {
            FileStream file = null;
            try
            {
                file = File.Create(Path.Combine(dir, meta.Name + "_s.cs"));
                file.WriteLine("using System;");
                file.WriteLine("using System.Collections.Generic;");
                file.WriteLine("using YiDian.EventBus;");
                file.WriteLine("using YiDian.EventBus.MQ;");
                file.WriteLine("using YiDian.EventBus.MQ.KeyAttribute;");
                file.WriteLine("namespace EventModels." + s_namespace);
                file.WriteLine("{");
                file.WriteLine("    public partial class " + meta.Name + ": " + typeof(IYiDianSeralize).Name);
                file.WriteLine("    {");
                file.WriteLine("        public uint ToBytes(WriteStream stream)");
                file.WriteLine("        {");
                file.WriteLine("            uint size = 5;");
                file.WriteLine("            var span = stream.Advance(4);");
                var dic = PropertyGroup(meta.Properties);
                file.WriteLine(string.Format("            stream.WriteByte({0});", dic.Count.ToString()));
                if (dic.TryGetValue(EventPropertyType.L_8, out List<PropertyMetaInfo> l8_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_8,{0});", l8_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_Date, out List<PropertyMetaInfo> ldate_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_Date,{0});", ldate_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_16, out List<PropertyMetaInfo> l16_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_16,{0});", l16_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_32, out List<PropertyMetaInfo> l32_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_32,{0});", l32_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_64, out List<PropertyMetaInfo> l64_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_64,{0});", l64_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_Str, out List<PropertyMetaInfo> lstr_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_Str,{0});", lstr_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_Array, out List<PropertyMetaInfo> larr_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_Array,{0});", larr_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_N, out List<PropertyMetaInfo> ln_props))
                {
                    file.WriteLine(string.Format("             size +=stream.WriteHeader(EventPropertyType.L_N,{0});", ln_props.Count.ToString()));
                }
                if (l8_props != null)
                {
                    l8_props = l8_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l8_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_Boolean)
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteByte({0} ? (byte)1 : (byte)0);", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteByte({0});", item.Name));
                        }
                    }
                }
                if (ldate_props != null)
                {
                    ldate_props = ldate_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in ldate_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        file.WriteLine(string.Format("             size +=stream.WriteDate({0});", item.Name));
                    }
                }
                if (l16_props != null)
                {
                    l16_props = l16_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l16_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt16)
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteUInt16({0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteInt16({0});", item.Name));
                        }
                    }
                }
                if (l32_props != null)
                {
                    l32_props = l32_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l32_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt32)
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteUInt32({0});", item.Name));
                        }
                        else if (item.Type.StartsWith(PropertyMetaInfo.P_Enum))
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteInt32((int){0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteInt32({0});", item.Name));
                        }
                    }
                }
                if (l64_props != null)
                {
                    l64_props = l64_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l64_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt64)
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteUInt64({0});", item.Name));
                        }
                        else if (item.Type == PropertyMetaInfo.P_Double)
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteDouble({0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("             size +=stream.WriteInt64({0});", item.Name));
                        }
                    }
                }
                if (lstr_props != null)
                {
                    lstr_props = lstr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in lstr_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_Date) file.WriteLine(string.Format("             size +=stream.WriteDate({0});", item.Name));
                        else file.WriteLine(string.Format("             size +=stream.WriteString({0});", item.Name));
                    }
                }
                if (larr_props != null)
                {
                    larr_props = larr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in larr_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        var arrtype = item.Type.Substring(6);
                        if (arrtype == PropertyMetaInfo.P_Byte) file.WriteLine(string.Format("             size +=stream.WriteArrayByte({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Date) file.WriteLine(string.Format("             size +=stream.WriteArrayDate({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Boolean) file.WriteLine(string.Format("             size +=stream.WriteArrayBool({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Double) file.WriteLine(string.Format("             size +=stream.WriteArrayDouble({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int16) file.WriteLine(string.Format("             size +=stream.WriteArrayInt16({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt16) file.WriteLine(string.Format("             size +=stream.WriteArrayUInt16({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int32) file.WriteLine(string.Format("             size +=stream.WriteArrayInt32({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt32) file.WriteLine(string.Format("             size +=stream.WriteArrayUInt32({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int64) file.WriteLine(string.Format("             size +=stream.WriteArrayInt64({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt64) file.WriteLine(string.Format("             size +=stream.WriteArrayUInt64({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_String) file.WriteLine(string.Format("             size +=stream.WriteArrayString({0});", item.Name));
                        else file.WriteLine(string.Format("             size +=stream.WriteEventArray({0});", item.Name));
                    }
                }
                if (ln_props != null)
                {
                    ln_props = ln_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in ln_props)
                    {
                        file.WriteLine(string.Format("             size +=stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        file.WriteLine(string.Format("             size +=stream.WriteEventObj({0});", item.Name));
                    }
                }
                file.WriteLine("            BitConverter.TryWriteBytes(span, size);");
                file.WriteLine("            return size;");
                file.WriteLine("        }");

                file.WriteLine("        public void BytesTo(ReadStream stream)");
                file.WriteLine("        {");
                file.WriteLine("            var headers = stream.ReadHeaders();");

                #region L8
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (l8_props != null)
                {
                    l8_props = l8_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l8_props)
                    {
                        if (item.Type == PropertyMetaInfo.P_Boolean)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadByte() == 1;continue;");
                            file.WriteLine("}");
                        }
                        else
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadByte();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                    stream.Advance(1);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region LDate
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_Date, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (ldate_props != null)
                {
                    ldate_props = ldate_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in ldate_props)
                    {
                        file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                        file.Write("{");
                        file.Write($" {item.Name} = stream.ReadDate();continue;");
                        file.WriteLine("}");
                    }
                }
                file.WriteLine("                    stream.Advance(11);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion
                #region L16
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_16, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (l16_props != null)
                {
                    l16_props = l16_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l16_props)
                    {
                        if (item.Type == PropertyMetaInfo.P_Int16)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadInt16();continue;");
                            file.WriteLine("}");
                        }
                        else
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadUInt16();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                    stream.Advance(2);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region L32
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_32, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (l32_props != null)
                {
                    l32_props = l32_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l32_props)
                    {
                        if (item.Type == PropertyMetaInfo.P_Int32)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadInt32();continue;");
                            file.WriteLine("}");
                        }
                        else if (item.Type.StartsWith(PropertyMetaInfo.P_Enum))
                        {
                            var type = item.Type.Substring(5);
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = ({type})stream.ReadInt32();continue;");
                            file.WriteLine("}");
                        }
                        else
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadUInt32();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                    stream.Advance(4);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region L_64
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_64, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (l64_props != null)
                {
                    l64_props = l64_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l64_props)
                    {
                        if (item.Type == PropertyMetaInfo.P_Int64)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadInt64();continue;");
                            file.WriteLine("}");
                        }
                        else if (item.Type == PropertyMetaInfo.P_Double)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadDouble();continue;");
                            file.WriteLine("}");
                        }
                        else if (item.Type == PropertyMetaInfo.P_UInt64)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadUInt64();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                    stream.Advance(8);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region L_Str
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_Str, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (lstr_props != null)
                {
                    lstr_props = lstr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in lstr_props)
                    {
                        if (item.Type == PropertyMetaInfo.P_Date)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadDate();continue;");
                            file.WriteLine("}");
                        }
                        else
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadString();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                     var c = stream.ReadInt32();stream.Advance(c);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region L_Array
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_Array, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (larr_props != null)
                {
                    larr_props = larr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in larr_props)
                    {
                        var arrtype = item.Type.Substring(6);
                        if (arrtype == PropertyMetaInfo.P_Byte)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayByte().ToArray();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Date)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayDate().To;continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Boolean)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayBool();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Double)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayDouble();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int16)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayInt16();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_UInt16)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayUInt16();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int32)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayInt32();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_UInt32)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayUInt32();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int64)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayInt64();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_UInt64)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayUInt64();continue;");
                            file.WriteLine("}");
                        }
                        else if (arrtype == PropertyMetaInfo.P_String)
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArrayString();continue;");
                            file.WriteLine("}");
                        }
                        else
                        {
                            file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                            file.Write("{");
                            file.Write($" {item.Name} = stream.ReadArray<{arrtype}>();continue;");
                            file.WriteLine("}");
                        }
                    }
                }
                file.WriteLine("                    var c = stream.ReadInt32();stream.Advance(c);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion

                #region L_N
                file.WriteLine("            if (headers.TryGetValue(EventPropertyType.L_N, out count))");
                file.WriteLine("            {");
                file.WriteLine("                for (var i = 0; i < count; i++)");
                file.WriteLine("                {");
                file.WriteLine("                    var index = stream.ReadByte();");
                if (ln_props != null)
                {
                    ln_props = ln_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in ln_props)
                    {
                        file.Write($"                    if (index == {item.SeralizeIndex.ToString()})");
                        file.Write("{");
                        file.Write($" {item.Name} = new {item.Type}();");
                        file.Write($" {item.Name}.BytesTo(stream);continue;");
                        file.WriteLine("}");
                    }
                }
                file.WriteLine("                    var l = stream.ReadInt32();");
                file.WriteLine("                    stream.Advance(l);");
                file.WriteLine("                }");
                file.WriteLine("            }");
                #endregion
                file.WriteLine("        }");


                file.WriteLine("        public uint Size()");
                file.WriteLine("        {");
                var size = 5 + dic.Count * 2 + meta.Properties.Count + (l8_props == null ? 0 : l8_props.Count) + (ldate_props == null ? 0 : ldate_props.Count * 11) + (l16_props == null ? 0 : l16_props.Count * 2) + (l32_props == null ? 0 : l32_props.Count * 4) + (l64_props == null ? 0 : l64_props.Count * 8);
                file.Write($"                var size={size}+");
                if (lstr_props != null)
                {
                    foreach (var s_pty in lstr_props)
                    {
                        if (s_pty.Type == PropertyMetaInfo.P_Date) file.Write("11+");
                        else file.Write($"WriteStream.GetStringSize({s_pty.Name})+");
                    }
                }
                if (larr_props != null)
                {
                    foreach (var item in larr_props)
                    {
                        var arrtype = item.Type.Substring(6);
                        if (arrtype == PropertyMetaInfo.P_Byte || arrtype == PropertyMetaInfo.P_Boolean)
                        {
                            file.Write($"WriteStream.GetValueArraySize(1,{item.Name})+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Date)
                        {
                            file.Write("11+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Double)
                        {
                            file.Write($"WriteStream.GetValueArraySize(8,{item.Name})+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int16 || arrtype == PropertyMetaInfo.P_UInt16)
                        {
                            file.Write($"WriteStream.GetValueArraySize(2,{item.Name})+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int32 || arrtype == PropertyMetaInfo.P_UInt32 || arrtype.StartsWith(PropertyMetaInfo.P_Enum))
                        {
                            file.Write($"WriteStream.GetValueArraySize(4,{item.Name})+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_Int64 || arrtype == PropertyMetaInfo.P_UInt64 || arrtype == PropertyMetaInfo.P_Double)
                        {
                            file.Write($"WriteStream.GetValueArraySize(8,{item.Name})+");
                        }
                        else if (arrtype == PropertyMetaInfo.P_String)
                        {
                            file.Write($"WriteStream.GetArrayStringSize({item.Name})+");
                        }
                        else
                        {
                            file.Write($"WriteStream.GetArrayEventObjSize({item.Name})+");
                        }
                    }
                }
                if (ln_props != null)
                {
                    foreach (var item in ln_props)
                    {
                        file.Write($"{item.Name}.Size()+");
                    }
                }
                file.WriteLine($" 0;");
                file.WriteLine($"                return size;");
                file.WriteLine("        }");


                file.WriteLine("    }");
                file.WriteLine("}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            finally
            {
                file?.Close();
            }
        }

        private Dictionary<EventPropertyType, List<PropertyMetaInfo>> PropertyGroup(List<PropertyMetaInfo> properties)
        {
            var dic = new Dictionary<EventPropertyType, List<PropertyMetaInfo>>();
            foreach (var p in properties)
            {
                if (p.Type == PropertyMetaInfo.P_Boolean || p.Type == PropertyMetaInfo.P_Byte)
                {
                    dic.TryAdd(EventPropertyType.L_8, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_8].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int16 || p.Type == PropertyMetaInfo.P_UInt16)
                {
                    dic.TryAdd(EventPropertyType.L_16, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_16].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Date)
                {
                    dic.TryAdd(EventPropertyType.L_Date, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_Date].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int32 || p.Type == PropertyMetaInfo.P_UInt32 || p.Type.StartsWith(PropertyMetaInfo.P_Enum))
                {
                    dic.TryAdd(EventPropertyType.L_32, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_32].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int64 || p.Type == PropertyMetaInfo.P_UInt64 || p.Type == PropertyMetaInfo.P_Double)
                {
                    dic.TryAdd(EventPropertyType.L_64, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_64].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_String)
                {
                    dic.TryAdd(EventPropertyType.L_Str, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_Str].Add(p);
                }
                else if (p.Type.StartsWith(PropertyMetaInfo.P_Array))
                {
                    dic.TryAdd(EventPropertyType.L_Array, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_Array].Add(p);
                }
                else
                {
                    dic.TryAdd(EventPropertyType.L_N, new List<PropertyMetaInfo>());
                    dic[EventPropertyType.L_N].Add(p);
                }
            }
            return dic;
        }
    }
}
