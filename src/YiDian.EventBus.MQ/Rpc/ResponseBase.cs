namespace YiDian.EventBus
{
    public class ResponseBase<T> : ResponseBase
    {
        public T Data { get; set; }
    }
    public class ResponseBase
    {
        public int ServerState { get; set; }
        public string ServerMsg { get; set; }
    }
}
