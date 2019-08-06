namespace YiDian.EventBus
{
    public interface IMqRpcClient
    {
        byte[] Request(string serverId, string uri, byte[] data);
    }
}
