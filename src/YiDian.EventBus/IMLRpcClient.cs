
using System;

namespace YiDian.EventBus
{
    /// <summary>
    /// 美林MQ RPC客户端
    /// </summary>
    public interface IMLRpcClient
    {
        string ServerName { get; }
        ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data);
        ResponseBase<T> Call<T>(string uri);
        bool IsConnect { get; set; }

        event Action ConnectionError;
    }
}
