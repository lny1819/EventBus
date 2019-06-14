using Autofac;
using Microsoft.Extensions.Logging;
using ML.EventBus;
using ML.EventBus.Abstractions;
using ML.EventBusMQ;
using ML.Fulturetrade.EventBus.Abstractions;
using System.Threading.Tasks;

namespace ML.MQ.RpcServer
{
    public class Creator : IPpcServerCreator
    {
        IRabbitMQPersistentConnection _conn = null;
        ILifetimeScope _autofac = null;
        IQpsCounter _qps = null;
        TaskFactory _factory = null;
        public Creator() { }
        public IPpcServer Create(RpcServerConfig config, object logger)
        {
            return new RPCServer(_conn, logger as ILogger, config, _autofac, _qps, _factory);
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
