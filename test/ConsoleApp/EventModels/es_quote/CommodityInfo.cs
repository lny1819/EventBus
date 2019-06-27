using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public class CommodityInfo: IMQEvent
    {
        public string ExchangeNo { get; set; }
        public string ExchangeName { get; set; }
        public CommodityType CommodityType { get; set; }
        public string CommodityNo { get; set; }
        public string CommodityName { get; set; }
        public string CommodityEngName { get; set; }
        public string TradeCurrency { get; set; }
        public double ContractSize { get; set; }
        public double StrikePriceTimes { get; set; }
        public double CommodityTickSize { get; set; }
        public Int32 MarketDot { get; set; }
        public Int32 CommodityDenominator { get; set; }
        public Int32 DeliveryDays { get; set; }
        public string AddOneTime { get; set; }
        public Int32 CommodityTimeZone { get; set; }
    }
}
