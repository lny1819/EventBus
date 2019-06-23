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
        public string Name { get; private set; }
        public void Run(ISoaServiceHost host, string name, string[] args)
        {
            //--loadevents -app history,userapi -path /data/his
            _logger = host.ServicesProvider.GetService<ILogger<MqEventsLocalBuild>>();
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
                    var mgr = sp.GetService<IAppEventsManager>();
                    var appnames = appname.Split(',');
                    foreach (var app in appnames)
                    {
                        var meta = mgr.ListEvents(app);
                        LocalBuildEvents(meta, path);
                    }
                    host.Exit(0);
                }
            }
        }

        private void LocalBuildEvents(AppMetas meta, string path)
        {
            var app = meta.Name;
            var version = meta.Version;
            path = Path.Combine(path, app);
            if (!Directory.Exists(path)) CreateFiles(meta.MetaInfos, app, Path.Combine(path, version));
            else
            {
                var versions = Directory.GetDirectories(path);
                foreach (var dir in versions)
                {
                    if (dir == version) return;
                }
                CreateFiles(meta.MetaInfos, app, Path.Combine(path, version));
            }
        }
        private void CreateFiles(List<ClassMeta> list, string s_namespace, string dir)
        {
            const string s_property = "public {0} {1} ";
            Directory.CreateDirectory(dir);
            foreach (var meta in list)
            {
                FileStream file = null;
                try
                {
                    file = File.Create(Path.Combine(dir, meta.Name + ".cs"));
                    file.WriteLine("using System;");
                    file.WriteLine("using System.Collections.Generic;");
                    file.WriteLine("using Events." + s_namespace + ";");
                    file.WriteLine("namespace Events." + s_namespace);
                    file.WriteLine("{");
                    file.WriteLine("public class " + meta.Name);
                    file.WriteLine("{");
                    foreach (var p in meta.Properties)
                    {
                        if (p.Type.StartsWith("arr_"))
                        {

                        }
                        else if (p.Type.StartsWith("list_"))
                        {

                        }
                        file.Write(string.Format(s_property, p.Type, p.Name));
                        file.WriteLine("{ get; set; }");
                    }
                    file.WriteLine("}}");
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
        }
    }
}
