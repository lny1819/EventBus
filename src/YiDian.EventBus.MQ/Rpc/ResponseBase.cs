namespace YiDian.EventBus
{
    /// <summary>
    /// RPC请求响应同一模型
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class ResponseBase<T> : ResponseBase
    {
        /// <summary>
        /// 响应数据
        /// </summary>
        public T Data { get; set; }
    }
    /// <summary>
    /// RPC请求响应同一模型
    /// </summary>
    public class ResponseBase
    {
        /// <summary>
        /// 响应状态码 200表示成功
        /// </summary>
        public int ServerState { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ServerMsg { get; set; }
    }
}
