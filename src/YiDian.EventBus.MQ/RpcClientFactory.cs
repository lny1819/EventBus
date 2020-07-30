using System.Collections.Concurrent;
using YiDian.EventBus.MQ.Rpc;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// RPC客户端工厂类接口
    /// </summary>
    public interface IRpcClientFactory
    {
        /// <summary>
        /// 创建一个MQRPC客户端实例
        /// </summary>
        /// <param name="serverId">RPC服务器编码</param>
        /// <param name="timeOut">请求超时时间，单位：秒</param>
        /// <returns></returns>
        IMQRpcClient Create(string serverId, int timeOut);
    }
    /// <summary>
    /// 
    /// </summary>
    public class RpcClientFactory : IRpcClientFactory
    {
        readonly MQRpcClientBase _rpc;
        ConcurrentDictionary<string, IMQRpcClient> factory;
        internal RpcClientFactory(MQRpcClientBase rpc)
        {
            _rpc = rpc;
            factory = new ConcurrentDictionary<string, IMQRpcClient>();
        }
        /// <summary>
        /// 创建一个MQRPC客户端实例
        /// </summary>
        /// <param name="serverId">RPC服务器编码</param>
        /// <param name="timeOut">请求超时时间，单位：秒</param>
        /// <returns></returns>
        public IMQRpcClient Create(string serverId, int timeOut)
        {
            var flag = factory.TryGetValue(serverId, out IMQRpcClient client);
            if (flag) return client;
            lock (factory)
            {
                flag = factory.TryGetValue(serverId, out client);
                if (!flag)
                {
                    client = new MQRpcClient(serverId, _rpc, timeOut);
                    factory.TryAdd(serverId, client);
                }
            }
            return client;
        }
    }
}
