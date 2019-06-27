using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public class QuoteBytes: IMQEvent
    {
        public Byte[] Datas { get; set; }
    }
}
