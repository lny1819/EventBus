namespace YiDian.EventBus.Abstractions
{
    public interface IMLRpcClientFactory
    {
        IMLRpcClient Create(string serverId);
    }
}
