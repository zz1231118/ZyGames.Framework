using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Framework.Injection;

namespace ZyGames.Framework.Services.Networking
{
    internal class ConnectionManager : IConnectionManager
    {
        private readonly IContainer container;
        private readonly ConcurrentDictionary<Address, ConnectionEntry> connections = new ConcurrentDictionary<Address, ConnectionEntry>();
        private readonly Func<Address, ConnectionEntry> connectionEntryFactory = key => new ConnectionEntry(key);

        public ConnectionManager(IContainer container)
        {
            this.container = container;
        }

        private ConnectionEntry GetConnectionEntry(Address endpoint)
        {
            return connections.GetOrAdd(endpoint, connectionEntryFactory);
        }

        public void Connected(Address address, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(address);
            lock (connectionEntry)
            {
                connectionEntry.Connections.Add(connection);
            }
        }

        public void ConnectionTerminated(Address address, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(address);
            lock (connectionEntry)
            {
                connectionEntry.Connections.Remove(connection);
            }
        }

        public Connection Allocate(Address endpoint)
        {
            var connectionEntry = GetConnectionEntry(endpoint);
            lock (connectionEntry)
            {
                var connection = connectionEntry.GetConnection();
                if (connection == null || !connection.IsConnected)
                {
                    if (connection != null && !connection.IsConnected)
                    {
                        connectionEntry.Connections.Remove(connection);
                        connection.Dispose();
                        connection = null;
                    }
                    if (connection == null)
                    {
                        var newConnection = new OutboundConnection(container);
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

        public void Return(Address endpoint, Connection connection)
        { }

        sealed class ConnectionEntry
        {
            private readonly Address endpoint;
            private readonly List<Connection> connections = new List<Connection>();

            [ThreadStatic]
            public int nextConnection;

            public ConnectionEntry(Address endpoint)
            {
                this.endpoint = endpoint;
            }

            public Address Endpoint => endpoint;

            public List<Connection> Connections => connections;

            public bool IsSufficient => connections.Count > 0;

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
