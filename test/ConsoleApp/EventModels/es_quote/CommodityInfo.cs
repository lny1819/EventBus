using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class CommodityInfo: IMQEvent
    {
        [SeralizeIndex(0)]
        public string ExchangeNo { get; set; }
        [SeralizeIndex(1)]
        public string ExchangeName { get; set; }
        [SeralizeIndex(2)]
        public CommodityType CommodityType { get; set; }
        [SeralizeIndex(3)]
        public string CommodityNo { get; set; }
        [SeralizeIndex(4)]
        public string CommodityName { get; set; }
        [SeralizeIndex(5)]
        public string CommodityEngName { get; set; }
        [SeralizeIndex(6)]
        public string TradeCurrency { get; set; }
        [SeralizeIndex(7)]
        public double ContractSize { get; set; }
        [SeralizeIndex(8)]
        public double StrikePriceTimes { get; set; }
        [SeralizeIndex(9)]
        public double CommodityTickSize { get; set; }
        [SeralizeIndex(10)]
        public Int32 MarketDot { get; set; }
        [SeralizeIndex(11)]
        public Int32 CommodityDenominator { get; set; }
        [SeralizeIndex(12)]
        public Int32 DeliveryDays { get; set; }
        [SeralizeIndex(13)]
        public string AddOneTime { get; set; }
        [SeralizeIndex(14)]
        public Int32 CommodityTimeZone { get; set; }
    }
}
