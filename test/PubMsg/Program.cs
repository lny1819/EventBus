using Microsoft.Extensions.Configuration;
using YiDian.Soa.Sp;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost.CreateBuilder()
               .ConfigApp(e => e.AddJsonFile("appsettings.json"))
               .RegisterMqConnection(e => e["mqconnstr"])
               .UseEventbus<StartUp>()
               .UseTopicEventBus<StartUp>()
               .UserStartUp<StartUp>()
               .Build(args)
               .Run(e => e["sysname"]);
        }
    }
}
