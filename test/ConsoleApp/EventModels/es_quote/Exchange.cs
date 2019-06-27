using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public class Exchange: IMQEvent
    {
        public string ExchangeNo { get; set; }
        public string ExchangeName { get; set; }
    }
}
