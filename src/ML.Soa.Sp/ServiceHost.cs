using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    public static class ServiceHost
    {
        public static SopServiceContainerBuilder CreateBuilder(IServiceCollection services = null)
        {
            var builder = new SopServiceContainerBuilder(services);
            return builder;
        }
    }
}
