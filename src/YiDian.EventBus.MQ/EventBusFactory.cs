using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// EventBus创建工厂
    /// </summary>
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
        private Dictionary<BusKey, IFanoutEventBus> FanoutBusDic { get; }

        readonly IServiceProvider _sp;
        readonly ILogger<IDirectEventBus> _logger;
        readonly ILogger<ITopicEventBus> _logger2;
        readonly ILogger<IFanoutEventBus> _logger3;
        /// <summary>
        /// 创建一个工厂实例
        /// </summary>
        /// <param name="source">RabbitMq连接源</param>
        /// <param name="scope">对象生命周期管理器</param>
        /// <param name="logger">IDirectEventBus日志</param>
        /// <param name="logger2">ITopicEventBus日志</param>
        /// <param name="logger3">IFanoutEventBus日志</param>
        public EventBusFactory(DefaultMqConnectSource source, IServiceProvider scope, ILogger<IDirectEventBus> logger, ILogger<ITopicEventBus> logger2, ILogger<IFanoutEventBus> logger3)
        {
            DirectBusDic = new Dictionary<BusKey, IDirectEventBus>(BusKey.Compare);
            TopicBusDic = new Dictionary<BusKey, ITopicEventBus>(BusKey.Compare);
            FanoutBusDic = new Dictionary<BusKey, IFanoutEventBus>(BusKey.Compare);
            var defaultDirect = scope.GetService<IDirectEventBus>();
            var defaultTopic = scope.GetService<ITopicEventBus>();
            if (defaultDirect != null) DirectBusDic.Add(new BusKey() { brokerName = defaultDirect.BROKER_NAME, connName = defaultDirect.ConnectionName }, defaultDirect);
            if (defaultTopic != null) TopicBusDic.Add(new BusKey() { brokerName = defaultTopic.BROKER_NAME, connName = defaultTopic.ConnectionName }, defaultTopic);
            _sp = scope;
            _source = source;
            _logger = logger;
            _logger2 = logger2;
            _logger3 = logger3;
        }
        /// <summary>
        /// 创建指定序列化，连接地址，交换机名称的IDirectEventBus类型EventBus
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="connSource">连接名称，由连接字符串中的name指定</param>
        /// <param name="brokerName">交换机名称</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public IDirectEventBus GetDirect<T>(T serializer = null, string connSource = "", string brokerName = "", int length = 100) where T : class, IEventSeralize, new()
        {
            var key = new BusKey(connSource, brokerName);
            if (DirectBusDic.TryGetValue(key, out IDirectEventBus bus)) return bus;
            var conn = _source.Get(connSource) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var connname = conn.Name;
            DirectEventBus eventbus;
            serializer ??= new T();
            if (string.IsNullOrEmpty(brokerName))
                eventbus = new DirectEventBus(_logger, _sp, conn, serializer, cacheCount: length);
            else
                eventbus = new DirectEventBus(brokerName, _logger, _sp, conn, serializer, cacheCount: length);
            DirectBusDic.TryAdd(key, eventbus);
            return eventbus;
        }
        /// <summary>
        ///  创建指定序列化，连接地址，交换机名称的ITopicEventBus类型EventBus
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="connSource">连接名称，由连接字符串中的name指定</param>
        /// <param name="brokerName">交换机名称</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public ITopicEventBus GetTopic<T>(T serializer = null, string connSource = "", string brokerName = "", int length = 100) where T : class, IEventSeralize, new()
        {
            var key = new BusKey(connSource, brokerName);
            if (TopicBusDic.TryGetValue(key, out ITopicEventBus bus)) return bus;
            var conn = _source.Get(connSource) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var connname = conn.Name;
            TopicEventBusMQ eventbus;
            serializer ??= new T();
            if (string.IsNullOrEmpty(brokerName))
                eventbus = new TopicEventBusMQ(_logger2, _sp, conn, serializer, cacheCount: length);
            else
                eventbus = new TopicEventBusMQ(brokerName, _logger2, _sp, conn, serializer, cacheCount: length);
            TopicBusDic.TryAdd(key, eventbus);
            return eventbus;
        }
        /// <summary>
        ///  创建指定序列化，连接地址，交换机名称的IFanoutEventBus类型EventBus
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="connSource">连接名称，由连接字符串中的name指定</param>
        /// <param name="brokerName">交换机名称</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public IFanoutEventBus GetFanout<T>(T serializer = null, string connSource = "", string brokerName = "", int length = 100) where T : class, IEventSeralize, new()
        {
            var key = new BusKey(connSource, brokerName);
            if (FanoutBusDic.TryGetValue(key, out IFanoutEventBus bus)) return bus;
            var conn = _source.Get(connSource) ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
            var connname = conn.Name;
            IFanoutEventBus eventbus;
            serializer ??= new T();
            if (string.IsNullOrEmpty(brokerName))
                eventbus = new FanoutEventBus(_logger3, _sp, serializer, conn, cacheCount: length);
            else
                eventbus = new FanoutEventBus(brokerName, _logger3, _sp, conn, serializer, cacheCount: length);
            FanoutBusDic.TryAdd(key, eventbus);
            return eventbus;
        }
    }
}
