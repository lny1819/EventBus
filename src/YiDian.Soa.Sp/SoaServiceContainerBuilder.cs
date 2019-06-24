using System;
using System.Collections.Generic;
using System.Linq;
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
        IConfiguration _config;
        string[] _args;
        public string Project_Dir { get; }
        public SoaServiceContainerBuilder(string[] args, IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
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
        public IConfiguration Config
        {
            get { return _config; }
            internal set
            {
                _config = value ?? throw new ArgumentNullException(nameof(Config));
                Services.AddSingleton(value);
            }
        }
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
