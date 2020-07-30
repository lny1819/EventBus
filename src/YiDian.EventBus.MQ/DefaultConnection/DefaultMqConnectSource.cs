using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace YiDian.EventBus.MQ.DefaultConnection
{
    /// <summary>
    /// 默认MQ连接管理池
    /// </summary>
    public class DefaultMqConnectSource
    {
        readonly Dictionary<string, DefaultRabbitMQPersistentConnection> factorys;

        private readonly int _retryCount;
        private readonly IEventBusSubManagerFactory default_sub_fact;
        /// <summary>
        /// 无法连接
        /// </summary>
        public event Action<IRabbitMQPersistentConnection, string> ConnectFail;
        /// <summary>
        /// 创建一个默认的MQ连接管理池实例
        /// </summary>
        /// <param name="retryCount">MQ断开重连尝试次数</param>
        /// <param name="subfactory">默认订阅管理工厂类</param>
        public DefaultMqConnectSource(int retryCount, IEventBusSubManagerFactory subfactory)
        {
            default_sub_fact = subfactory ?? throw new ArgumentNullException(nameof(IEventBusSubManagerFactory));
            factorys = new Dictionary<string, DefaultRabbitMQPersistentConnection>(StringComparer.CurrentCultureIgnoreCase);
            _retryCount = retryCount;
        }
        /// <summary>
        /// 使用订阅管理工厂和指定的连接字符串  创建一个默认的MQ连接
        /// </summary>
        /// <param name="mqConnstr">MQ连接字符串</param>
        /// <param name="subFactory">订阅管理工厂</param>
        public void Create(string mqConnstr, IEventBusSubManagerFactory subFactory = null)
        {
            subFactory ??= default_sub_fact;
            var conn = CreateConnect(mqConnstr, out string source_name);
            if (factorys.TryGetValue(source_name, out _)) throw new Exception($"repeat create mq connection by the name {source_name}");
            var mqconn = new DefaultRabbitMQPersistentConnection(conn, source_name, _retryCount, subFactory);
            if (!factorys.TryAdd(source_name, mqconn))
            {
                mqconn.Dispose();
                return;
            }
            mqconn.ConnectFail += Mqconn_ConnectFail;
        }

        private void Mqconn_ConnectFail(object sender, string e)
        {
            var conn = (IRabbitMQPersistentConnection)sender;
            ConnectFail?.Invoke(conn, e);
            if (ConnectFail == null) throw new Exception("the rabbitmq connection named " + conn.Name + " failes");
        }
        /// <summary>
        /// 获取指定名称的MQ连接
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IRabbitMQPersistentConnection Get(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "default";
            if (factorys.TryGetValue(name, out DefaultRabbitMQPersistentConnection factory)) return factory;
            return null;
        }
        /// <summary>
        /// 是否已经存在指定名称的MQ连接
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return factorys.ContainsKey(name);
        }
        private ConnectionFactory CreateConnect(string connstr, out string name)
        {
            name = "default";
            string server = "";
            int port = 0;
            string user = "";
            string pwd = "";
            string vhost = "/";
            bool isasync = false;
            var s_arr = connstr.Split(';');
            if (s_arr.Length < 4) throw new ArgumentException("连接字符串格式不正确");
            foreach (var s in s_arr)
            {
                var kv = s.Split('=');
                if (kv[0] == "server")
                {
                    var srs = kv[1].Split(':');
                    server = srs[0];
                    if (srs.Length > 1) port = int.Parse(srs[1]);
                }
                else if (kv[0] == "user") user = kv[1];
                else if (kv[0] == "password") pwd = kv[1];
                else if (kv[0] == "vhost") vhost = kv[1];
                else if (kv[0] == "isasync") isasync = bool.Parse(kv[1]);
                else if (kv[0] == "name") name = kv[1];
            }
            var factory = new ConnectionFactory()
            {
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(3),
                DispatchConsumersAsync = isasync,
                RequestedConnectionTimeout = 10000,
                RequestedHeartbeat = 7,
                HostName = server,
                Password = pwd,
                UserName = user,
                Port = port == 0 ? 5672 : port,
                VirtualHost = vhost
            };
            return factory;
        }
    }
}
