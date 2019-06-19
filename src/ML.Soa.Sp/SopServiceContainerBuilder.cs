using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{

    public class SopServiceContainerBuilder
    {
        public SopServiceContainerBuilder(IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
        }

        public IServiceCollection Services { get; }
        internal IConfigurationRoot Config { get; set; }
        internal Type StartUp { get; set; }

        public ISoaServiceHost Build(string[] args = null)
        {
            var host = new DefaultServiceHost(this, args);
            return host;
        }
    }
}
