using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RspUseAction: IMQEvent
    {
        [SeralizeIndex(0)]
        public UInt32 SessionId { get; set; }
        [SeralizeIndex(1)]
        public Boolean IsLast { get; set; }
        [SeralizeIndex(2)]
        public ErrorCode ErrorCode { get; set; }
        [SeralizeIndex(3)]
        public string ErrMsg { get; set; }
        [SeralizeIndex(4)]
        public RspUserOrderInfo Data { get; set; }
    }
}
