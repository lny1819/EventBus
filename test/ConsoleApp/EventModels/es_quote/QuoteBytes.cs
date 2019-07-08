using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class QuoteBytes: IMQEvent
    {
        [SeralizeIndex(0)]
        public Byte[] Datas { get; set; }
    }
}
