using YiDian.Soa.Sp;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using System.Text;
using Microsoft.Extensions.Logging;

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
            const string s_property = "        public {0} {1} ";
            const string list_property = "        public List<{0}> {1} ";
            const string attr_property = "        [KeyIndex({0})]";
            Directory.CreateDirectory(dir);
            foreach (var meta in appmeta.MetaInfos)
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
                        file.WriteLine("    public class " + meta.Name + ": " + typeof(IMQEvent).Name);
                    else
                        file.WriteLine("    public class " + meta.Name);
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
                        if (p.Type.StartsWith("list#"))
                        {
                            var list_type = "";
                            var args = p.Type.Split('#');
                            for (var i = 1; i < args.Length; i++)
                            {
                                list_type += args[i];
                                if (i != args.Length - 1) list_type += ',';
                            }
                            file.Write(string.Format(list_property, list_type, p.Name));
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
    }
}
