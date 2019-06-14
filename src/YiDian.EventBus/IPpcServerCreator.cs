using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IPpcServerCreator
    {
        IPpcServer Create(RpcServerConfig config, object logger);
        void Init(object conn, object autofac, IQpsCounter qps,  TaskFactory factory);
    }
}
