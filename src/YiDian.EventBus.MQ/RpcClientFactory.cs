using System.Collections.Concurrent;

namespace YiDian.EventBus.MQ
{
    public class RpcClientFactory : IMqRpcClientFactory
    {
        readonly IRpcClient _rpc;
        ConcurrentDictionary<string, IMQRpcClient> factory;
        public RpcClientFactory(IRpcClient rpc)
        {
            _rpc = rpc;
            factory = new ConcurrentDictionary<string, IMQRpcClient>();
        }
        public IMQRpcClient Create(string serverId)
        {
            var flag = factory.TryGetValue(serverId, out IMQRpcClient client);
            if (flag) return client;
            lock (factory)
            {
                flag = factory.TryGetValue(serverId, out client);
                if (!flag)
                {
                    client = new MQRpcClient(serverId, _rpc);
                    factory.TryAdd(serverId, client);
                }
            }
            return client;
        }
    }
}
