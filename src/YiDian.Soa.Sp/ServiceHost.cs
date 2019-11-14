using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    public static class ServiceHost
    {
        public static SoaServiceContainerBuilder CreateBuilder(string[] args, IServiceCollection services = null, ContainerBuilder container = null)
        {
            var builder = new SoaServiceContainerBuilder(args, services, container);
            return builder;
        }
    }
}
