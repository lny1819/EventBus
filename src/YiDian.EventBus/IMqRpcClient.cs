using System;

namespace YiDian.EventBus
{
    public interface IMQRpcClient
    {
        string ServerName { get; }
        ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data);
        ResponseBase<T> Call<T>(string uri);
        bool IsConnect { get; set; }

        event EventHandler ConnectionError;
    }
}
