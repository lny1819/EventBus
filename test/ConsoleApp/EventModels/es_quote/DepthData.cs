using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public class DepthData: IMQEvent
    {
        public string CurrencyNo { get; set; }
        public string InstrumentID { get; set; }
        public string ExchangeID { get; set; }
        public string CommodityNo { get; set; }
        public CommodityType CommodityType { get; set; }
        public double LastPrice { get; set; }
        public double PreSettlementPrice { get; set; }
        public double PreClosePrice { get; set; }
        public double PreOpenInterest { get; set; }
        public double OpenPrice { get; set; }
        public double HighestPrice { get; set; }
        public double LowestPrice { get; set; }
        public Int32 Volume { get; set; }
        public double Turnover { get; set; }
        public double OpenInterest { get; set; }
        public double ClosePrice { get; set; }
        public double SettlementPrice { get; set; }
        public double UpperLimitPrice { get; set; }
        public double LowerLimitPrice { get; set; }
        public double PreDelta { get; set; }
        public double CurrDelta { get; set; }
        public string InTime { get; set; }
        public string UpdateTime { get; set; }
        public string AllowTradeTime { get; set; }
        public DateTime TradingDay { get; set; }
        public double BidPrice1 { get; set; }
        public Int32 BidVolume1 { get; set; }
        public double AskPrice1 { get; set; }
        public Int32 AskVolume1 { get; set; }
        public double BidPrice2 { get; set; }
        public Int32 BidVolume2 { get; set; }
        public double AskPrice2 { get; set; }
        public Int32 AskVolume2 { get; set; }
        public double BidPrice3 { get; set; }
        public Int32 BidVolume3 { get; set; }
        public double AskPrice3 { get; set; }
        public Int32 AskVolume3 { get; set; }
        public double BidPrice4 { get; set; }
        public Int32 BidVolume4 { get; set; }
        public double AskPrice4 { get; set; }
        public Int32 AskVolume4 { get; set; }
        public double BidPrice5 { get; set; }
        public Int32 BidVolume5 { get; set; }
        public double AskPrice5 { get; set; }
        public Int32 AskVolume5 { get; set; }
        public double AveragePrice { get; set; }
        public Int32 TotalVolume { get; set; }
    }
}
