using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class ReqUserOrderAction: IMQEvent
    {
        [SeralizeIndex(0)]
        public string LocalOrderNo { get; set; }
        [SeralizeIndex(1)]
        public double StopProfit { get; set; }
        [SeralizeIndex(2)]
        public double StopLoss { get; set; }
        [SeralizeIndex(3)]
        public double CommitPrice { get; set; }
        [SeralizeIndex(4)]
        public UInt32 CommitSize { get; set; }
        [SeralizeIndex(5)]
        public UserAction Action { get; set; }
    }
}
