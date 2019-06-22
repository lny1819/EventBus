using System.Collections.Generic;
using YiDian.Soa.Sp;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using System.Text;
using System.Linq;

namespace YiDian.EventBus.MQ
{


    internal class MqEventsLocalBuild : IAppRun
    {
        public string Name { get; private set; }
        public void Run(ISoaServiceHost host, string name, string[] args)
        {
            //--loadevents -app history,userapi -path /data/his
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
            const string s_property = "public {0} {1} { get; set; }";
            Directory.CreateDirectory(dir);
            foreach (var meta in list)
            {
                var file = File.Create(Path.Combine(dir, meta.Name));
                var sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("namespace Events." + s_namespace + "");
                sb.AppendLine("{");
                sb.AppendLine("public class " + meta.Name);
                sb.AppendLine("{");
                foreach (var p in meta.Properties)
                {
                    if (p.Type.StartsWith("arr_"))
                    {

                    }
                    else if (p.Type.StartsWith("list_"))
                    {

                    }
                    sb.Append(string.Format(s_property, p.Type, p.Name));
                }
                sb.AppendLine("}}");
            }
        }
    }
}
