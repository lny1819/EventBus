using Autofac;
using Microsoft.Extensions.Logging;
using ML.EventBus;
using ML.EventBus.Abstractions;
using ML.EventBusMQ;
using ML.Fulturetrade.EventBus.Abstractions;
using System;
using System.Threading.Tasks;

namespace ML.Soa.Sp
{
    public class RpcServerCreator
    {
        readonly IPpcServerCreator _creator;
        internal RpcServerCreator(IRabbitMQPersistentConnection conn, ILifetimeScope autofac, IQpsCounter qps, TaskFactory factory, IPpcServerCreator creator)
        {
            _creator = creator ?? throw new ArgumentNullException(nameof(IPpcServerCreator));
            autofac = autofac ?? throw new ArgumentNullException(nameof(ILifetimeScope));
            conn = conn ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            qps = qps ?? throw new ArgumentNullException(nameof(IQpsCounter));
            factory = factory ?? throw new ArgumentNullException(nameof(TaskFactory));
            _creator.Init(conn, autofac, qps, factory);
        }
        public IPpcServer Create(ILogger logger, RpcServerConfig config)
        {
            logger = logger ?? throw new ArgumentNullException(nameof(ILogger));
            config = config ?? throw new ArgumentNullException(nameof(RpcServerConfig));
            return _creator.Create(config, logger);
        }
    }
}
