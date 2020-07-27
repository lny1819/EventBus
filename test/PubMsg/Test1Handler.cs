using EventModels.depthdata;
using System.Threading.Tasks;
using YiDian.EventBus;

namespace ConsoleApp
{
    public class Test1Handler : IEventHandler<DepthData>
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test1Handler rec");
            return Task.FromResult(true);
        }
    }
    public class Test2Handler : IEventHandler<DepthData>
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test2Handler rec");
            return Task.FromResult(true);
        }
    }
    public class Test3Handler : IEventHandler<DepthData>
    {
        public Task<bool> Handle(DepthData @event)
        {
            System.Console.WriteLine("Test3Handler rec");
            return Task.FromResult(true);
        }
    }
}