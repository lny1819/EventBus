using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.userinfo
{
    public partial class RspUserOrderInfo
    {
        [SeralizeIndex(0)]
        public string LocalOrderNo { get; set; }
        [SeralizeIndex(1)]
        public string Exchange { get; set; }
        [SeralizeIndex(2)]
        public string ContractId { get; set; }
        [SeralizeIndex(3)]
        public string Commodity { get; set; }
        [SeralizeIndex(4)]
        public string ServiceNo { get; set; }
        [SeralizeIndex(5)]
        public UInt32 Size { get; set; }
        [SeralizeIndex(6)]
        public double Price { get; set; }
        [SeralizeIndex(7)]
        public OrderState State { get; set; }
        [SeralizeIndex(8)]
        public UInt32 FillSize { get; set; }
        [SeralizeIndex(9)]
        public OrderActType Action { get; set; }
    }
}
