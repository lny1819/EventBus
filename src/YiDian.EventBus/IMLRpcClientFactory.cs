namespace YiDian.EventBus
{
    public interface IMLRpcClientFactory
    {
        IMLRpcClient Create(string serverId);
    }
}
