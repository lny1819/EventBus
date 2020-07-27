using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.depthdata
{
    public partial class DepthData: IMQEvent
    {
        [SeralizeIndex(0)]
        public string CurrencyNo { get; set; }
        [SeralizeIndex(1)]
        public double AskPrice1 { get; set; }
        [SeralizeIndex(2)]
        public double AskPrice2 { get; set; }
        [SeralizeIndex(3)]
        public double AskPrice3 { get; set; }
        [SeralizeIndex(4)]
        public double AskPrice4 { get; set; }
        [SeralizeIndex(5)]
        public double AskPrice5 { get; set; }
        [SeralizeIndex(6)]
        public UInt64 AskVolume1 { get; set; }
        [SeralizeIndex(7)]
        public UInt64 AskVolume2 { get; set; }
        [SeralizeIndex(8)]
        public UInt64 AskVolume3 { get; set; }
        [SeralizeIndex(9)]
        public UInt64 AskVolume4 { get; set; }
        [SeralizeIndex(10)]
        public UInt64 AskVolume5 { get; set; }
        [SeralizeIndex(11)]
        public double AveragePrice { get; set; }
        [SeralizeIndex(12)]
        public double BidPrice1 { get; set; }
        [SeralizeIndex(13)]
        public double BidPrice2 { get; set; }
        [SeralizeIndex(14)]
        public double BidPrice3 { get; set; }
        [SeralizeIndex(15)]
        public double BidPrice4 { get; set; }
        [SeralizeIndex(16)]
        public double BidPrice5 { get; set; }
        [SeralizeIndex(17)]
        public UInt64 BidVolume1 { get; set; }
        [SeralizeIndex(18)]
        public UInt64 BidVolume2 { get; set; }
        [SeralizeIndex(19)]
        public UInt64 BidVolume3 { get; set; }
        [SeralizeIndex(20)]
        public UInt64 BidVolume4 { get; set; }
        [SeralizeIndex(21)]
        public UInt64 BidVolume5 { get; set; }
        [SeralizeIndex(22)]
        public double ClosePrice { get; set; }
        [KeyIndex(1)]
        [SeralizeIndex(23)]
        public string CommodityNo { get; set; }
        [SeralizeIndex(24)]
        public CommodityType CommodityType { get; set; }
        [KeyIndex(0)]
        [SeralizeIndex(25)]
        public string ExchangeID { get; set; }
        [SeralizeIndex(26)]
        public double HighestPrice { get; set; }
        [SeralizeIndex(27)]
        public double HisHighPrice { get; set; }
        [SeralizeIndex(28)]
        public double HisLowPrice { get; set; }
        [KeyIndex(2)]
        [SeralizeIndex(29)]
        public string InstrumentID { get; set; }
        [SeralizeIndex(30)]
        public double LastPrice { get; set; }
        [SeralizeIndex(31)]
        public UInt64 ImpliedBidQty { get; set; }
        [SeralizeIndex(32)]
        public double LowestPrice { get; set; }
        [SeralizeIndex(33)]
        public UInt64 OpenInterest { get; set; }
        [SeralizeIndex(34)]
        public double OpenPrice { get; set; }
        [SeralizeIndex(35)]
        public double PreClosePrice { get; set; }
        [SeralizeIndex(36)]
        public double PreDelta { get; set; }
        [SeralizeIndex(37)]
        public double CurrDelta { get; set; }
        [SeralizeIndex(38)]
        public double TurnoverRate { get; set; }
        [SeralizeIndex(39)]
        public UInt64 PreOpenInterest { get; set; }
        [SeralizeIndex(40)]
        public double PreSettlementPrice { get; set; }
        [SeralizeIndex(41)]
        public double SettlementPrice { get; set; }
        [SeralizeIndex(42)]
        public UInt64 TotalVolume { get; set; }
        [SeralizeIndex(43)]
        public double Turnover { get; set; }
        [SeralizeIndex(44)]
        public DateTime LocalTime { get; set; }
        [SeralizeIndex(45)]
        public UInt64 Volume { get; set; }
        [SeralizeIndex(46)]
        public double ImpliedAskPrice { get; set; }
        [SeralizeIndex(47)]
        public UInt64 ImpliedAskQty { get; set; }
        [SeralizeIndex(48)]
        public double ImpliedBidPrice { get; set; }
        [SeralizeIndex(49)]
        public TradingState TradingState { get; set; }
        [SeralizeIndex(50)]
        public UInt64 Q5DAvgQty { get; set; }
        [SeralizeIndex(51)]
        public double PERatio { get; set; }
        [SeralizeIndex(52)]
        public UInt64 TotalValue { get; set; }
        [SeralizeIndex(53)]
        public double NegotiableValue { get; set; }
        [SeralizeIndex(54)]
        public double PositionTrend { get; set; }
        [SeralizeIndex(55)]
        public double ChangeSpeed { get; set; }
        [SeralizeIndex(56)]
        public double ChangeRate { get; set; }
        [SeralizeIndex(57)]
        public double Swing { get; set; }
        [SeralizeIndex(58)]
        public UInt64 TotalBidQty { get; set; }
        [SeralizeIndex(59)]
        public UInt64 TotalAskQty { get; set; }
        [SeralizeIndex(60)]
        public DateTime InTime { get; set; }
        [SeralizeIndex(61)]
        public string Source { get; set; }
        [SeralizeIndex(62)]
        public DateTime TradingDay { get; set; }
        [SeralizeIndex(63)]
        public string TradingTime { get; set; }
        [SeralizeIndex(64)]
        public UInt64 InsideQty { get; set; }
        [SeralizeIndex(65)]
        public UInt64 OutsideQty { get; set; }
        [SeralizeIndex(66)]
        public double ChangeValue { get; set; }
    }
}
