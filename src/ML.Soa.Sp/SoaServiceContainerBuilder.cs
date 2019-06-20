·using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{

    public class SoaServiceContainerBuilder
    {
        readonly List<IAppRun> appRuns;
        public SoaServiceContainerBuilder(IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
            appRuns = new List<IAppRun>();
        }
        public void RegisterRun(IAppRun run)
        {
            if (appRuns.Contains(run))
                return;
        }
        public List<IAppRun> GetAllAppRun()
        {
            return appRuns;
        }
        public IServiceCollection Services { get; }
        public IConfigurationRoot Config { get; internal set; }
        internal Type StartUp { get; set; }

        public ISoaServiceHost Build(string[] args = null)
        {
            var host = new DefaultServiceHost(this, args);
            return host;
        }
    }
}
