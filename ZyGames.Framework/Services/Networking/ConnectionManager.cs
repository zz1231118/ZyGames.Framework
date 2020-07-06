using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            lock (connectionEntry)
            {
                connectionEntry.Connections.Add(connection);
            }
        }

        public void ConnectionTerminated(SlioAddress address, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(address);
            lock (connectionEntry)
            {
                connectionEntry.Connections.Remove(connection);
            }
        }

        public Connection GetConnection(SlioAddress endpoint)
        {
            var connectionEntry = GetConnectionEntry(endpoint);
            lock (connectionEntry)
            {
                var connection = connectionEntry.GetConnection();
                if (connection == null || !connection.IsConnected)
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

                        connection = newConnection;
                        connectionEntry.Connections.Add(newConnection);
                    }
                }
                return connection;
            }
        }

        class ConnectionEntry
        {
            private readonly SlioAddress endpoint;
            private readonly List<Connection> connections = new List<Connection>();

            [ThreadStatic]
            public int nextConnection;

            public ConnectionEntry(SlioAddress endpoint)
            {
                this.endpoint = endpoint;
            }

            public SlioAddress Endpoint => endpoint;

            public List<Connection> Connections => connections;

            public Connection GetConnection()
            {
                if (connections.Count == 0)
                {
                    return null;
                }

                nextConnection = (nextConnection + 1) % connections.Count;
                return connections[nextConnection];
            }
        }
    }
}
