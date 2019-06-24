using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{

    public class SoaServiceContainerBuilder
    {
        readonly List<IAppRun> appRuns;
        IConfiguration _config;
        string[] _args;
#if DEBUG
        public string Project_Dir { get; }
#endif
        public SoaServiceContainerBuilder(string[] args, IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
            appRuns = new List<IAppRun>();
            _args = args;
#if DEBUG
            for (var i = 0; i < _args.Length; i++)
            {
                if (_args[i] == "-pj_dir")
                {
                    Project_Dir = _args[i + 1];
                }
            }
#endif
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
