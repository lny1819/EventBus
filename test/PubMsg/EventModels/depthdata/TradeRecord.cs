using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.depthdata
{
    public partial class TradeRecord: IMQEvent
    {
        [SeralizeIndex(0)]
        public string CommodityNo { get; set; }
        [SeralizeIndex(1)]
        public string ExchangeID { get; set; }
        [SeralizeIndex(2)]
        public string InstrumentID { get; set; }
        [SeralizeIndex(3)]
        public double LastPrice { get; set; }
        [SeralizeIndex(4)]
        public string Oper { get; set; }
        [SeralizeIndex(5)]
        public string InTime { get; set; }
        [SeralizeIndex(6)]
        public UInt64 TotalVolume { get; set; }
        [SeralizeIndex(7)]
        public DateTime TradeTime { get; set; }
        [SeralizeIndex(8)]
        public UInt64 Volume { get; set; }
        [SeralizeIndex(9)]
        public Int32 ZCVolume { get; set; }
    }
}
