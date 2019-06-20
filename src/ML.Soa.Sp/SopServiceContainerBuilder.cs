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

        public object UseRabbitMq(Func<object, object> p)
        {
            throw new NotImplementedException();
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
