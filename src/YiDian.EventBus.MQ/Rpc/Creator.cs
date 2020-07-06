using Autofac;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using YiDian.Soa.Sp;

namespace YiDian.EventBus.MQ.Rpc
{
    public class Creator
    {
        IRabbitMQPersistentConnection _conn = null;
        ILifetimeScope _autofac = null;
        IQpsCounter _qps = null;
        TaskFactory _factory = null;
        public Creator() { }
        public RPCServer Create(RpcServerConfig config, object logger)
        {
            return new RPCServer(_conn, logger as ILogger, config, _autofac, _qps);
        }

        public void Init(object conn, object autofac, IQpsCounter qps, TaskFactory factory)
        {
            _conn = conn as IRabbitMQPersistentConnection;
            _autofac = autofac as ILifetimeScope;
            _qps = qps;
            _factory = factory;
        }
    }
}
