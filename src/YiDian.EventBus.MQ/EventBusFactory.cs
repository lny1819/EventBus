using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.EventBus.MQ
{
    public class EventBusFactory
    {
        struct BusKey
        {
            public string connName;
            public string brokerName;
            public BusKey(string connName, string brokerName)
            {
                this.connName = connName;
                this.brokerName = brokerName;
            }

            internal static readonly _Compare Compare;
            static BusKey()
            {
                Compare = new _Compare();
            }
            internal class _Compare : IEqualityComparer<BusKey>
            {
                public bool Equals(BusKey x, BusKey y)
                {
                    return string.Compare(x.connName, y.connName, true) == 0 && string.Compare(x.brokerName, y.brokerName, true) == 0;
                }
                public int GetHashCode(BusKey obj)
                {
                    return 0;
                }
            }
        }
        readonly DefaultMqConnectSource _source;

        private Dictionary<BusKey, IDirectEventBus> DirectBusDic { get; }
        private Dictionary<BusKey, ITopicEventBus> TopicBusDic { get; }

        readonly IServiceProvider _sp;
        readonly ILogger<IDirectEventBus> _logger;
        readonly ILogger<ITopicEventBus> _logger2;

        public EventBusFactory(DefaultMqConnectSource source, IServiceProvider scope, ILogger<IDirectEventBus> logger, ILogger<ITopicEventBus> logger2)
        {
            DirectBusDic = new Dictionary<BusKey, IDirectEventBus>(BusKey.Compare);
            TopicBusDic = new Dictionary<BusKey, ITopicEventBus>(BusKey.Compare);
            _sp = scope;
            _source = source;
            _logger = logger;
            _logger2 = logger2;
        }
        public IDirectEventBus GetDirect(string connSource = "", string brokerName = "", int length = 100)
        {
            var key = new BusKey(connSource, brokerName);
            if (DirectBusDic.TryGetValue(key, out IDirectEventBus bus)) return bus;
            var conn = _source.Get(connSource) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var connname = conn.Name;
            DirectEventBus eventbus;
            if (string.IsNullOrEmpty(brokerName))
                eventbus = new DirectEventBus(_logger, _sp, conn, cacheCount: length);
            else
                eventbus = new DirectEventBus(brokerName, _logger, _sp, conn, cacheCount: length);
            DirectBusDic.TryAdd(key, eventbus);
            return eventbus;
        }
        public ITopicEventBus GetTopic(string connSource = "", string brokerName = "", int length = 100)
        {
            var key = new BusKey(connSource, brokerName);
            if (TopicBusDic.TryGetValue(key, out ITopicEventBus bus)) return bus;
            var conn = _source.Get(connSource) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var connname = conn.Name;
            TopicEventBusMQ eventbus;
            if (string.IsNullOrEmpty(brokerName))
                eventbus = new TopicEventBusMQ(_logger2, _sp, conn, cacheCount: length);
            else
                eventbus = new TopicEventBusMQ(brokerName, _logger2, _sp, conn, cacheCount: length);
            TopicBusDic.TryAdd(key, eventbus);
            return eventbus;
        }
    }
}
