using EventModels.depthdata;
using System;
using System.Text;
using System.Threading.Tasks;
using YiDian.EventBus;

namespace ConsoleApp
{
    public class Test1Handler : IEventHandler<DepthData>, IBytesHandler
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test1Handler rec");
            return Task.FromResult(true);
        }

        public Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas)
        {
            Console.WriteLine("Test1Handler IBytesHandler " + routingKey + " " + Encoding.UTF8.GetString(datas.ToArray()));
            return Task.FromResult(true);
        }
    }
    public class Test2Handler : IEventHandler<DepthData>, IBytesHandler
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test2Handler rec");
            return Task.FromResult(true);
        }
        public Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas)
        {
            Console.WriteLine("Test2Handler IBytesHandler " + routingKey + " " + Encoding.UTF8.GetString(datas.ToArray()));
            return Task.FromResult(true);
        }
    }
    public class Test3Handler : IEventHandler<DepthData>, IBytesHandler
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test3Handler rec");
            return Task.FromResult(true);
        }
        public Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas)
        {
            Console.WriteLine("Test3Handler IBytesHandler " + routingKey + " " + Encoding.UTF8.GetString(datas.ToArray()));
            return Task.FromResult(true);
        }
    }
    public class Test4Handler : IEventHandler<DepthData>, IBytesHandler
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test4Handler rec");
            return Task.FromResult(true);
        }
        public Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas)
        {
            Console.WriteLine("Test4Handler IBytesHandler " + routingKey + " " + Encoding.UTF8.GetString(datas.ToArray()));
            return Task.FromResult(true);
        }
    }
}