using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    public static class ServiceHost
    {
        public static SoaServiceContainerBuilder CreateBuilder(string[] args, IServiceCollection services = null)
        {
            var builder = new SoaServiceContainerBuilder(args,services);
            return builder;
        }
    }
}
