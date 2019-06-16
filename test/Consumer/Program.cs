using Microsoft.Extensions.Configuration;
using YiDian.Soa.Sp;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            MqServiceHost.CreateBuilder()
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
