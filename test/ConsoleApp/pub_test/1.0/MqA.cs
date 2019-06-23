using System;
using System.Collections.Generic;
using  YiDian.EventBus.MQ.KeyAttribute;
namespace Events.pub_test
{
    public class MqA
    {
        [KeyIndex(0)]
        public string A { get; set; }
        public string B { get; set; }
        public Int32 ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
    }
}
