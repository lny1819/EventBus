using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace YiDian.EventBus.MQ.DefaultConnection
{
    public class DefaultRabbitMQPersistentConnection
       : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;
        private readonly int _retryCount;
        IConnection _connection;
        bool _disposed;
        readonly object sync_root = new object();

        public event EventHandler OnConnectRecovery;

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, IAppEventsManager eventsManager, IEventSeralize seralize, int retryCount = 5)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(IConnectionFactory));
            Seralizer = seralize ?? throw new ArgumentNullException(nameof(IEventSeralize));
            EventsManager = eventsManager ?? throw new ArgumentNullException(nameof(IAppEventsManager));
            _logger = new ConsoleLog();
            _retryCount = retryCount;
        }
        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }
        public IAppEventsManager EventsManager { get; }
        public IEventSeralize Seralizer { get; }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            lock (sync_root)
            {
                if (IsConnected) return true;
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex.Message);
                    }
                );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.RecoverySucceeded += _connection_RecoverySucceeded;
                    _connection.ConnectionRecoveryError += _connection_ConnectionRecoveryError;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.LocalPort.ToString()} and is subscribed to failure events");

                    return true;
                }
                else
                {
                    _logger.LogWarning("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void _connection_ConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            _logger.LogWarning("A RabbitMQ Connection RecoveryError");
        }

        private void _connection_RecoverySucceeded(object sender, EventArgs e)
        {
            _logger.LogWarning("A RabbitMQ Connection RecoverySucceeded");
            OnConnectRecovery?.Invoke(this, null);
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        }
    }
}
