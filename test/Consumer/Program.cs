using Microsoft.Extensions.Configuration;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost.CreateBuilder()
               .ConfigApp(e => e.AddJsonFile("appsettings.json"))
               .UserStartUp<StartUp>()
               .Build(args)
               .Run(e => e["sysname"]);
        }
    }
}
