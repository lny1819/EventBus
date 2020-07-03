using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class UserInsertOrder: IMQEvent
    {
        [SeralizeIndex(1)]
        public double StopProfit { get; set; }
        [SeralizeIndex(2)]
        public double StopLoss { get; set; }
        [SeralizeIndex(3)]
        public OrderDirection Side { get; set; }
        [SeralizeIndex(4)]
        public OrderTimeType TimeType { get; set; }
        [SeralizeIndex(5)]
        public OrderType OrderType { get; set; }
        [SeralizeIndex(6)]
        public string Exchange { get; set; }
        [SeralizeIndex(7)]
        public string Commodity { get; set; }
        [SeralizeIndex(8)]
        public string Contract { get; set; }
        [SeralizeIndex(9)]
        public string UserInfo { get; set; }
        [SeralizeIndex(10)]
        public string Account { get; set; }
        [SeralizeIndex(11)]
        public double CommitPrice { get; set; }
        [SeralizeIndex(12)]
        public UInt32 CommitSize { get; set; }
        [SeralizeIndex(13)]
        public string FromPositionId { get; set; }
        [SeralizeIndex(14)]
        public Boolean Locked { get; set; }
        [SeralizeIndex(15)]
        public Boolean IsCover { get; set; }
    }
}
