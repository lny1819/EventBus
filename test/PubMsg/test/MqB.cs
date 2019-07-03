using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.MyTest
{
    public partial class MqB
    {
        [SeralizeIndex(0)]
        public string C { get; set; }
        [SeralizeIndex(1)]
        public string[] D { get; set; }
    }
    public enum MqType
    {
        ZS = 1,
        LS = 2
    }
}
