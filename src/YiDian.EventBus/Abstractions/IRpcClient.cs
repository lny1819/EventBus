namespace YiDian.EventBus.Abstractions
{
    public interface IRpcClient
    {
        /// <summary>
        /// Rpc请求客户端
        /// </summary>
        /// <param name="serverId">服务端ID</param>
        /// <param name="uri">服务端请求地址 以 . 割开</param>
        /// <param name="data">请求数据</param>
        /// <returns></returns>
        byte[] Request(string serverId, string uri, byte[] data);
    }
}
