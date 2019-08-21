using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace YiDian.EventBus.MQ.DefaultConnection
{
    public class DefaultMqConnectSource
    {
        readonly Dictionary<string, DefaultRabbitMQPersistentConnection> factorys;

        public IEventSeralize Seralizer { get; }
        public IEventBusSubManagerFactory SubsFactory { get; }

        private readonly int _retryCount;

        public DefaultMqConnectSource( IEventSeralize seralize, ILogger<IEventBusSubManager> sub_logger, int retryCount, IEventBusSubManagerFactory factory)
        {
            factorys = new Dictionary<string, DefaultRabbitMQPersistentConnection>(StringComparer.CurrentCultureIgnoreCase);
            Seralizer = seralize ?? throw new ArgumentNullException(nameof(IEventSeralize));
            SubsFactory = factory ?? throw new ArgumentNullException(nameof(IEventBusSubManagerFactory));
            _retryCount = retryCount;
        }
        public DefaultRabbitMQPersistentConnection Create(string mqConnstr)
        {
            var conn = CreateConnect(mqConnstr, out string source_name);
            if (factorys.TryGetValue(source_name, out DefaultRabbitMQPersistentConnection factory)) return factory;
            var mqconn = new DefaultRabbitMQPersistentConnection(conn, source_name, _retryCount, SubsFactory, Seralizer);
            if (!factorys.TryAdd(source_name, mqconn)) mqconn.Dispose();
            return mqconn;
        }
        public IRabbitMQPersistentConnection Get(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "default";
            if (factorys.TryGetValue(name, out DefaultRabbitMQPersistentConnection factory)) return factory;
            return null;
        }
        public bool Contains(string name)
        {
            return factorys.ContainsKey(name);
        }
        private static ConnectionFactory CreateConnect(string connstr, out string name)
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
                RequestedConnectionTimeout = 30000,
                RequestedHeartbeat = 17,
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
