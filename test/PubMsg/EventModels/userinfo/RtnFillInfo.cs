using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RtnFillInfo: IMQEvent
    {
        [SeralizeIndex(0)]
        public string LocalOrderNo { get; set; }
        [SeralizeIndex(1)]
        public string ServiceOrderNo { get; set; }
        [SeralizeIndex(2)]
        public string ServerMatchNo { get; set; }
        [SeralizeIndex(3)]
        public UInt32 FillSize { get; set; }
        [SeralizeIndex(4)]
        public double FillPrice { get; set; }
        [SeralizeIndex(5)]
        public string TradeTime { get; set; }
        [SeralizeIndex(6)]
        public double UpperFeeValue { get; set; }
        [SeralizeIndex(7)]
        public string Commodity { get; set; }
        [SeralizeIndex(8)]
        public string Contract { get; set; }
        [SeralizeIndex(9)]
        public string Exchange { get; set; }
    }
}
