using System;

namespace YiDian.EventBus.MQ.Rpc
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

    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class FromBodyAttribute : Attribute
    {
        // This is a positional argument
        public FromBodyAttribute()
        {
        }
    }
}
