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

    internal class MqEventsLocalBuild : IAppRun
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
            var versionFile = Path.Combine(dir, appmeta.Version.ToString() + ".v");
            var v_file = File.OpenWrite(versionFile);
            var json = appmeta.ToJson();
            v_file.Write(json);
            v_file.Close();
        }
        private void CreateMainClassFile(string dir, string s_namespace, ClassMeta meta)
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
                        file.Write(string.Format(list_property, type, p.Name));
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
        private void CreateSeralizeClassFile(string dir, string s_namespace, ClassMeta meta)
        {
            FileStream file = null;
            try
            {
                file = File.Create(Path.Combine(dir, meta.Name + "_s.cs"));
                file.WriteLine("using System;");
                file.WriteLine("using System.Collections.Generic;");
                file.WriteLine("using YiDian.EventBus;");
                file.WriteLine("using YiDian.EventBus.MQ.KeyAttribute;");
                file.WriteLine("namespace EventModels." + s_namespace);
                file.WriteLine("{");
                file.WriteLine("    public partial class " + meta.Name + ": " + typeof(IYiDianSeralize).Name);
                file.WriteLine("    {");
                file.WriteLine("        public uint ToBytes(WriteStream stream)");
                file.WriteLine("        {");
                file.WriteLine("            var size = Size;");
                file.WriteLine("            stream.WriteUInt32(size);");
                var dic = PropertyGroup(meta.Properties);
                file.WriteLine(string.Format("            stream.WriteByte({0});", dic.Count.ToString()));
                if (dic.TryGetValue(EventPropertyType.L_8, out List<PropertyMetaInfo> l8_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_8,{0});", l8_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_16, out List<PropertyMetaInfo> l16_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_16,{0});", l16_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_32, out List<PropertyMetaInfo> l32_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_32,{0});", l32_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_64, out List<PropertyMetaInfo> l64_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_64,{0});", l64_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_Str, out List<PropertyMetaInfo> lstr_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_Str,{0});", lstr_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_Array, out List<PropertyMetaInfo> larr_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_Array,{0});", larr_props.Count.ToString()));
                }
                if (dic.TryGetValue(EventPropertyType.L_N, out List<PropertyMetaInfo> ln_props))
                {
                    file.WriteLine(string.Format("            stream.WriteHeader(EventPropertyType.L_N,{0});", ln_props.Count.ToString()));
                }
                if (l8_props != null)
                {
                    l8_props = l8_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l8_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_Boolean)
                        {
                            file.WriteLine(string.Format("            stream.WriteByte({0} ? (byte)1 : (byte)0);", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("            stream.WriteByte({0});", item.Name));
                        }
                    }
                }
                if (l16_props != null)
                {
                    l16_props = l16_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l16_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt16)
                        {
                            file.WriteLine(string.Format("            stream.WriteUInt16({0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("            stream.WriteInt16({0});", item.Name));
                        }
                    }
                }
                if (l32_props != null)
                {
                    l32_props = l32_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l32_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt32)
                        {
                            file.WriteLine(string.Format("            stream.WriteUInt32({0});", item.Name));
                        }
                        else if (item.Type == PropertyMetaInfo.P_Enum)
                        {
                            file.WriteLine(string.Format("            stream.WriteInt32((int){0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("            stream.WriteInt32({0});", item.Name));
                        }
                    }
                }
                if (l64_props != null)
                {
                    l64_props = l64_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in l64_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        if (item.Type == PropertyMetaInfo.P_UInt64)
                        {
                            file.WriteLine(string.Format("            stream.WriteUInt64({0});", item.Name));
                        }
                        else if (item.Type == PropertyMetaInfo.P_Double)
                        {
                            file.WriteLine(string.Format("            stream.WriteDouble({0});", item.Name));
                        }
                        else
                        {
                            file.WriteLine(string.Format("            stream.WriteInt64({0});", item.Name));
                        }
                    }
                }
                if (lstr_props != null)
                {
                    lstr_props = lstr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in lstr_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        file.WriteLine(string.Format("            stream.WriteString({0});", item.Name));
                    }
                }
                if (larr_props != null)
                {
                    larr_props = larr_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in larr_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        var arrtype = item.Type.Substring(6);
                        if (arrtype == PropertyMetaInfo.P_Byte) file.WriteLine(string.Format("            stream.WriteArrayByte({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Date) file.WriteLine(string.Format("            stream.WriteArrayDate({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Boolean) file.WriteLine(string.Format("            stream.WriteArrayBool({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Double) file.WriteLine(string.Format("            stream.WriteArrayDouble({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int16) file.WriteLine(string.Format("            stream.WriteArrayInt16({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt16) file.WriteLine(string.Format("            stream.WriteArrayUInt16({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int32) file.WriteLine(string.Format("            stream.WriteArrayInt32({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt32) file.WriteLine(string.Format("            stream.WriteArrayUInt32({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_Int64) file.WriteLine(string.Format("            stream.WriteArrayInt64({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_UInt64) file.WriteLine(string.Format("            stream.WriteArrayUInt64({0});", item.Name));
                        else if (arrtype == PropertyMetaInfo.P_String) file.WriteLine(string.Format("            stream.WriteArrayString({0});", item.Name));
                        else file.WriteLine(string.Format("            stream.WriteEventArray({0});", item.Name));
                    }
                }
                if (ln_props != null)
                {
                    ln_props = ln_props.OrderBy(x => x.SeralizeIndex).ToList();
                    foreach (var item in ln_props)
                    {
                        file.WriteLine(string.Format("            stream.WriteIndex({0});", item.SeralizeIndex.ToString()));
                        file.WriteLine(string.Format("            stream.WriteEventObj({0});", item.Name));
                    }
                }
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
            var dic = new Dictionary<EventPropertyType, List<PropertyMetaInfo>>
            {
                { EventPropertyType.L_8, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_16, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_32, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_64, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_Str, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_Array, new List<PropertyMetaInfo>() },
                { EventPropertyType.L_N, new List<PropertyMetaInfo>() }
            };
            foreach (var p in properties)
            {
                if (p.Type == PropertyMetaInfo.P_Boolean || p.Type == PropertyMetaInfo.P_Byte)
                {
                    dic[EventPropertyType.L_8].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int16 || p.Type == PropertyMetaInfo.P_UInt16)
                {
                    dic[EventPropertyType.L_16].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int32 || p.Type == PropertyMetaInfo.P_UInt32 || p.Type.StartsWith(PropertyMetaInfo.P_Enum))
                {
                    dic[EventPropertyType.L_32].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Int64 || p.Type == PropertyMetaInfo.P_UInt64 || p.Type == PropertyMetaInfo.P_Double)
                {
                    dic[EventPropertyType.L_32].Add(p);
                }
                else if (p.Type == PropertyMetaInfo.P_Date || p.Type == PropertyMetaInfo.P_String)
                {
                    dic[EventPropertyType.L_Str].Add(p);
                }
                else if (p.Type.StartsWith(PropertyMetaInfo.P_Array))
                {
                    dic[EventPropertyType.L_Array].Add(p);
                }
                else dic[EventPropertyType.L_N].Add(p);
            }
            return dic;
        }
    }
}
