using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.pub_test
{
    public class MqA: IMQEvent
    {
        [KeyIndex(0)]
        public string A { get; set; }
        public string B { get; set; }
        public MqB QB { get; set; }
        public List<String> LC { get; set; }
        public String[] D { get; set; }
        public MqType Type { get; set; }
    }
}
