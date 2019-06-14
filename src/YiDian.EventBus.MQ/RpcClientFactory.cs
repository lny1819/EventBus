using YiDian.EventBus.Abstractions;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace YiDian.EventBusMQ
{
    public class RpcClientFactory : IMLRpcClientFactory
    {
        readonly IRpcClient _rpc;
        ConcurrentDictionary<string, IMLRpcClient> factory;
        public RpcClientFactory(IRpcClient rpc)
        {
            _rpc = rpc;
            factory = new ConcurrentDictionary<string, IMLRpcClient>();
        }
        public IMLRpcClient Create(string serverId)
        {
            var flag = factory.TryGetValue(serverId, out IMLRpcClient client);
            if (flag) return client;
            lock (factory)
            {
                flag = factory.TryGetValue(serverId, out client);
                if (!flag)
                {
                    client = new MLRpcClient(serverId, _rpc);
                    factory.TryAdd(serverId, client);
                }
            }
            return client;
        }
    }
}
