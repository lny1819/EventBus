using Autofac;
using Microsoft.Extensions.Configuration;
using System;

namespace YiDian.Soa.Sp
{
    public interface ISoaServiceHost
    {
        IServiceProvider ServicesProvider { get; }
        int Run(Func<IConfiguration, string> getName, bool background = false, IServiceProvider provider = null);
        IServiceProvider ConfigContainerBuilder(ContainerBuilder container);
        void Exit(int code);
    }
}
