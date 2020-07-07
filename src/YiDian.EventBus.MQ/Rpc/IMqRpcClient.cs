using System;
using System.Threading.Tasks;

namespace YiDian.EventBus.MQ.Rpc
{
    public interface IMQRpcClient
    {
        string ServerId { get; }
        public bool IsConnected { get; }
        ResponseBase<TOut> Call<Tin, TOut>(string uri, Tin data, ContentType type);
        Task<ResponseBase<TOut>> CallAsync<Tin, TOut>(string uri, Tin data, ContentType type);
        ResponseBase<TOut> Call<TOut>(string uri);
        Task<ResponseBase<TOut>> CallAsync<TOut>(string uri);
        void Cancel(long mid);
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
