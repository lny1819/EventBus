using System.Collections.Generic;
using YiDian.Soa.Sp;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using System.Text;
using System.Linq;
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
                        var meta = _eventsManager.ListEvents(app);
                        LocalBuildEvents(meta, path);
                    }
                    host.Exit(0);
                }
            }
            scope.Dispose();
        }

        private void LocalBuildEvents(AppMetas meta, string path)
        {
            var app = meta.Name;
            var version = meta.Version;
            path = Path.Combine(path, "EventModels", app);
            if (!Directory.Exists(path)) CreateFiles(meta, path);
            else
            {
                var versionFile = Path.Combine(path, version + ".v");
                if (File.Exists(versionFile)) return;
                foreach (var file in Directory.GetFiles(path, "*.cs"))
                    File.Delete(file);
                CreateFiles(meta, path);
            }
        }
        private void CreateFiles(AppMetas appmeta, string dir)
        {
            var s_namespace = appmeta.Name;
            const string s_property = "        public {0} {1} ";
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
                    file.WriteLine("using YiDian.EventBus.MQ.KeyAttribute;");
                    file.WriteLine("namespace EventModels." + s_namespace);
                    file.WriteLine("{");
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
                        if (p.Type.StartsWith("arr_"))
                        {

                        }
                        else if (p.Type.StartsWith("list_"))
                        {

                        }
                        file.Write(string.Format(s_property, p.Type, p.Name));
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
            var versionFile = Path.Combine(dir, appmeta.Version + ".v");
            var v_file = File.OpenWrite(versionFile);
            var json = appmeta.ToJson();
            v_file.Write(json);
            v_file.Close();
        }
    }
}
