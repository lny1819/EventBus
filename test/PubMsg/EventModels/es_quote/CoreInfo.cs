using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class CoreInfo: IMQEvent
    {
        [SeralizeIndex(0)]
        public string AccountNo { get; set; }
        [SeralizeIndex(1)]
        public string Password { get; set; }
        [SeralizeIndex(2)]
        public string Address { get; set; }
        [SeralizeIndex(3)]
        public string CoreName { get; set; }
    }
}
