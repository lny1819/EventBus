using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    public static class ServiceHost
    {
        public static SoaServiceContainerBuilder CreateBuilder(IServiceCollection services = null)
        {
            var builder = new SoaServiceContainerBuilder(services);
            return builder;
        }
    }
}
