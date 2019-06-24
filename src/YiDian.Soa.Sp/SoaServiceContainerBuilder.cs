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
        public SoaServiceContainerBuilder(string[] args, IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
            appRuns = new List<IAppRun>();
            _args = args;
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
