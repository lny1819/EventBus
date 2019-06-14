namespace ML.MQ.RpcServer.Abstractions
{
    public class ResponseBase
    {
        /// <summary>
        /// 20000 握手响应
        /// 401  指定方法不存在
        /// 500  服务器内部异常
        /// </summary>
        public int ServerState { get; set; }
        public string ServerMsg { get; set; }
        public object Data { get; set; }
    }
}
