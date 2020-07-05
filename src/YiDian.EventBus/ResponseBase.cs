namespace YiDian.EventBus
{
    public class ResponseBase<T> : ievent
    {
        public int ServerState { get; set; }
        public string ServerMsg { get; set; }
        public T Data { get; set; }
    }
}
