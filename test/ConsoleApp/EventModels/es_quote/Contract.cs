using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public class Contract: IMQEvent
    {
        public string ExchangeNo { get; set; }
        public CommodityType CommodityType { get; set; }
        public string CommodityNo { get; set; }
        public string InstrumentID { get; set; }
        public double MarginValue { get; set; }
        public double FreeValue { get; set; }
        public string ContractExpDate { get; set; }
        public string LastTradeDate { get; set; }
        public string FirstNoticeDate { get; set; }
        public string ContractName { get; set; }
    }
}
