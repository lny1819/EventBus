using Microsoft.Extensions.Configuration;
using System;

namespace YiDian.Soa.Sp
{
    public interface ISoaServiceHost
    {
        IServiceProvider ServicesProvider { get; }
        IConfiguration Configuration { get; }
        int Run(Func<IConfiguration, string> getName, bool background = false);
        event Action<Exception> UnCatchedException;
        void OnException(Exception e);
        void Exit(int code);
    }
}
