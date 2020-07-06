using System.Collections.Generic;

namespace YiDian.EventBus.MQ.Rpc.Abstractions
{
    public class Request
    {
        public RouteAction Action { get; set; }
        public Dictionary<string, string> Headers { get; internal set; }
        public string QueryString { get; set; }
        public IEventSeralize Seralize { get; internal set; }
        public long ContentLength { get; internal set; }
    }
}
