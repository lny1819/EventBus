using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.MyTest
{
    public partial class MqA: IMQEvent
    {
        [KeyIndex(0)]
        [SeralizeIndex(0)]
        public string PropertyA { get; set; }
        [SeralizeIndex(1)]
        public string PropertyB { get; set; }
        [SeralizeIndex(2)]
        public MqB PropertyQB { get; set; }
        [SeralizeIndex(3)]
        public string[] PropertyLC { get; set; }
        [SeralizeIndex(4)]
        public string[] PropertyD { get; set; }
        [SeralizeIndex(5)]
        public MqType Type { get; set; }
        [SeralizeIndex(6)]
        public Boolean Flag { get; set; }
        [SeralizeIndex(7)]
        public DateTime Date { get; set; }
        [SeralizeIndex(8)]
        public MqB[] QBS { get; set; }
        [SeralizeIndex(9)]
        public Int32 Index { get; set; }
        [SeralizeIndex(10)]
        public double Amount { get; set; }
        [SeralizeIndex(11)]
        public double[] Amounts { get; set; }
    }
}
