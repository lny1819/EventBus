using System;
using System.Threading.Tasks;

namespace YiDian.EventBus.MQ.Rpc
{
    /// <summary>
    /// MQRPC客户端
    /// </summary>
    public interface IMQRpcClient
    {
        /// <summary>
        /// RPC服务器编码
        /// </summary>
        string ServerId { get; }
        /// <summary>
        /// 是否连接成功
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 发起一次请求
        /// </summary>
        /// <typeparam name="Tin">请求入参类型</typeparam>
        /// <typeparam name="TOut">请求出参类型</typeparam>
        /// <param name="uri">请求地址</param>
        /// <param name="data">入参数据</param>
        /// <param name="type">入参数据格式</param>
        /// <returns></returns>
        ResponseBase<TOut> Call<Tin, TOut>(string uri, Tin data, ContentType type);
        /// <summary>
        /// 发起一次异步请求
        /// </summary>
        /// <typeparam name="Tin">请求入参类型</typeparam>
        /// <typeparam name="TOut">请求出参类型</typeparam>
        /// <param name="uri">请求地址</param>
        /// <param name="data">入参数据</param>
        /// <param name="type">入参数据格式</param>
        Task<ResponseBase<TOut>> CallAsync<Tin, TOut>(string uri, Tin data, ContentType type);
        /// <summary>
        /// 发起一次请求
        /// </summary>
        /// <typeparam name="TOut">请求出参类型</typeparam>
        /// <param name="uri">请求地址</param>
        /// <returns></returns>
        ResponseBase<TOut> Call<TOut>(string uri);
        /// <summary>
        /// 发起一次异步请求
        /// </summary>
        /// <typeparam name="TOut">请求出参类型</typeparam>
        /// <param name="uri">请求地址</param>
        /// <returns></returns>
        Task<ResponseBase<TOut>> CallAsync<TOut>(string uri);
        /// <summary>
        /// 取消请求
        /// </summary>
        /// <param name="mid">请求编号</param>
        void Cancel(long mid);
    }
    /// <summary>
    /// MQRpc服务端
    /// </summary>
    public interface IRPCServer
    {
        /// <summary>
        /// 服务端编码
        /// </summary>
        string ServerId { get; }
    }
    /// <summary>
    /// 指示参数来源
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class FromBodyAttribute : Attribute
    {
        /// <summary>
        /// 指示参数来源
        /// </summary>
        public FromBodyAttribute()
        {
        }
    }
}
