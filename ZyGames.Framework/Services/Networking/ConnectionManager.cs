using System;
using System.Collections.Concurrent;
using ZyGames.Framework.Services.Networking;

namespace ZyGames.Framework.Services
{
    internal class ConnectionManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<SlioAddress, ConnectionEntry> connections = new ConcurrentDictionary<SlioAddress, ConnectionEntry>();
        private readonly Func<SlioAddress, ConnectionEntry> connectionEntryFactory = key => new ConnectionEntry(key);

        public ConnectionManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        private ConnectionEntry GetConnectionEntry(SlioAddress endpoint)
        {
            return connections.GetOrAdd(endpoint, connectionEntryFactory);
        }

        public void Connected(SlioAddress address, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(address);
            if (connectionEntry.Connection == null)
            {
                lock (connectionEntry)
                {
                    if (connectionEntry.Connection == null)
                    {
                        connectionEntry.Connection = connection;
                    }
                }
            }
        }

        public void ConnectionTerminated(SlioAddress address, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(address);
            if (connectionEntry.Connection == connection)
            {
                lock (connectionEntry)
                {
                    if (connectionEntry.Connection == connection)
                    {
                        connectionEntry.Connection = null;
                    }
                }
            }
        }

        public Connection GetConnection(SlioAddress endpoint)
        {
            var connectionEntry = GetConnectionEntry(endpoint);
            var connection = connectionEntry.Connection;
            if (connection == null || !connection.IsConnected)
            {
                lock (connectionEntry)
                {
                    if (connection != null && !connection.IsConnected)
                    {
                        connection.Dispose();
                        connection = null;
                    }
                    if (connection == null)
                    {
                        var newConnection = new OutboundConnection(serviceProvider);
                        try
                        {
                            newConnection.Connect(endpoint);
                        }
                        catch
                        {
                            newConnection.Dispose();
                            throw;
                        }

                        connectionEntry.Connection = newConnection;
                    }

                    connection = connectionEntry.Connection;
                }
            }
            return connection;
        }

        class ConnectionEntry
        {
            private readonly SlioAddress endpoint;

            public ConnectionEntry(SlioAddress endpoint)
            {
                this.endpoint = endpoint;
            }

            public SlioAddress Endpoint => endpoint;

            public Connection Connection { get; set; }
        }
    }
}
