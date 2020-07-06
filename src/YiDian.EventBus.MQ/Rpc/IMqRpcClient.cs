using System;
using YiDian.EventBus.MQ.Rpc;

namespace YiDian.EventBus
{
    public interface IMQRpcClient
    {
        string ServerId { get; }
        ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data, ContentType type);
        void Cancel(long mid);
        bool IsConnect { get; set; }
        event EventHandler ConnectionError;
    }
    public interface IRPCServer
    {
        string ServerId { get; }
    }
}
