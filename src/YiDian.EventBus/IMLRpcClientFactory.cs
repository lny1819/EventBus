namespace YiDian.EventBus
{
    public interface IMqRpcClientFactory
    {
        IMQRpcClient Create(string serverId);
    }
}
