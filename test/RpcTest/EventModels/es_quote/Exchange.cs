using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class Exchange: IMQEvent
    {
        [SeralizeIndex(0)]
        public string ExchangeNo { get; set; }
        [SeralizeIndex(1)]
        public string ExchangeName { get; set; }
    }
}
