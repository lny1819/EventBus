using System;
using System.Collections.Generic;
using YiDian.EventBus.MQ.Rpc.Route;

namespace YiDian.EventBus.MQ.Rpc.Abstractions
{
    public class Request
    {
        public ActionInfo Action { get; set; }
        public Dictionary<string, string> Headers { get; internal set; }
        public Uri Url { get; set; }
        public IEventSeralize Seralize { get; internal set; }
        public long ContentLength { get; internal set; }
        public ReadOnlyMemory<byte> Body { get; internal set; }
    }
}
