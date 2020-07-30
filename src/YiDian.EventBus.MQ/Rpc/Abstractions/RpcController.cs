

namespace YiDian.EventBus.MQ.Rpc.Abstractions
{
    /// <summary>
    /// RPC服务端用户控制器基类
    /// </summary>
    public class RpcController
    {
        /// <summary>
        /// 请求上下文
        /// </summary>
        public Request Request { get; set; }
    }
}
