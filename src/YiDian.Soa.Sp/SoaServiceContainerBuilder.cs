using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    /// <summary>
    /// Project_Dir 依赖命令行传入 -pj_dir 参数
    /// </summary>
    public class SoaServiceContainerBuilder
    {
        readonly List<IAppRun> appRuns;
        Dictionary<string, object> tags;
        string[] _args;
        public string Project_Dir { get; }
        internal SoaServiceContainerBuilder(string[] args, IServiceCollection services , ContainerBuilder container )
        {
            tags = new Dictionary<string, object>();
            Services = services ?? new ServiceCollection();
            Container = container ?? new ContainerBuilder();
            appRuns = new List<IAppRun>();
            _args = args;
            for (var i = 0; i < _args.Length; i++)
            {
                if (_args[i] == "-pj_dir")
                {
                    Project_Dir = _args[i + 1];
                }
            }
        }
        public bool SetTag(string name, object tag)
        {
            return tags.TryAdd(name, tag);
        }
        public object GetTag(string name)
        {
            var f = tags.TryGetValue(name, out object tag);
            if (f) return tag;
            return null;
        }
        public void RegisterRun(IAppRun run)
        {
            if (appRuns.Contains(run))
                return;
            appRuns.Add(run);
        }
        public List<IAppRun> GetAllAppRun()
        {
            return appRuns;
        }
        public IServiceCollection Services { get; }
        public ContainerBuilder Container { get; }
        internal Type StartUp { get; set; }
        public string[] GetArgs()
        {
            return _args;
        }
        public ISoaServiceHost Build()
        {
            var host = new DefaultServiceHost(this, _args);
            return host;
        }

        public void AppendArgs(string[] command)
        {
            _args = _args.ToList().Concat(command).ToArray();
        }
    }
}
