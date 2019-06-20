using Microsoft.Extensions.Configuration;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost.CreateBuilder()
               .ConfigApp(e => e.AddJsonFile("appsettings.json"))
               .UseRabbitMq(e => e["mqconnstr"])
               .UserStartUp<StartUp>()
               .UseDirectEventBus<MySeralize>()
               .UseTopicEventBus<MySeralize>()
               .Build(args)
               .Run(e => e["sysname"]);
        }
    }
}
