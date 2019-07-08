using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class Contract: IMQEvent
    {
        [SeralizeIndex(0)]
        public string ExchangeNo { get; set; }
        [SeralizeIndex(1)]
        public CommodityType CommodityType { get; set; }
        [SeralizeIndex(2)]
        public string CommodityNo { get; set; }
        [SeralizeIndex(3)]
        public string InstrumentID { get; set; }
        [SeralizeIndex(4)]
        public double MarginValue { get; set; }
        [SeralizeIndex(5)]
        public double FreeValue { get; set; }
        [SeralizeIndex(6)]
        public string ContractExpDate { get; set; }
        [SeralizeIndex(7)]
        public string LastTradeDate { get; set; }
        [SeralizeIndex(8)]
        public string FirstNoticeDate { get; set; }
        [SeralizeIndex(9)]
        public string ContractName { get; set; }
    }
}
