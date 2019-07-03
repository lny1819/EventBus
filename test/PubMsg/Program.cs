using Microsoft.Extensions.Configuration;
using System;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var b = new byte[2];
            var span = new Span<byte>(b, 0, 2);
            BitConverter.TryWriteBytes(span, (short)1282);
            var xx = BitConverter.ToUInt16(new byte[] { 5, 2 });

            ServiceHost.CreateBuilder(args)
               .ConfigApp(e => e.AddJsonFile("appsettings.json"))
               .UserStartUp<StartUp>()
               .Build()
               .Run(e => e["sysname"]);
        }
    }
}
