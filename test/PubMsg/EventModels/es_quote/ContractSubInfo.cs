using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class ContractSubInfo: IMQEvent
    {
        [SeralizeIndex(0)]
        public string Exchange { get; set; }
        [SeralizeIndex(1)]
        public string Commodity { get; set; }
        [SeralizeIndex(2)]
        public string Contract { get; set; }
        [SeralizeIndex(3)]
        public string CoreID { get; set; }
        [SeralizeIndex(4)]
        public Boolean Success { get; set; }
    }
}
