using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.zs
{
    public partial class TestMqEvent: IMQEvent
    {
        [SeralizeIndex(0)]
        public string Name { get; set; }
        [SeralizeIndex(1)]
        public Int32 Age { get; set; }
    }
}
