using Microsoft.Extensions.DependencyInjection;

namespace YiDian.Soa.Sp
{
    public static class MqServiceHost
    {
        public static MlSopServiceContainerBuilder CreateBuilder(IServiceCollection services = null)
        {
            var builder = new MlSopServiceContainerBuilder(services);
            return builder;
        }
    }
}
