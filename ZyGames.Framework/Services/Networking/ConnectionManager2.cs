using System;
using System.Collections.Concurrent;
using Framework.Injection;

namespace ZyGames.Framework.Services.Networking
{
    internal class ConnectionManager2 : IConnectionManager
    {
        private readonly IContainer container;
        private readonly ConcurrentDictionary<Address, ConnectionEntry> connections = new ConcurrentDictionary<Address, ConnectionEntry>();
        private readonly Func<Address, ConnectionEntry> connectionEntryFactory = key => new ConnectionEntry(key);

        public ConnectionManager2(IContainer container)
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
            connectionEntry.Connections.Enqueue(connection);
        }

        public void ConnectionTerminated(Address address, Connection connection)
        { }

        public Connection Allocate(Address endpoint)
        {
            var connectionEntry = GetConnectionEntry(endpoint);
            Connection connection;
            while (connectionEntry.Connections.TryDequeue(out connection))
            {
                if (connection.IsConnected) break;
                else connection.Dispose();
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
            }
            return connection;
        }

        public void Return(Address endpoint, Connection connection)
        {
            var connectionEntry = GetConnectionEntry(endpoint);
            connectionEntry.Connections.Enqueue(connection);
        }

        sealed class ConnectionEntry
        {
            private readonly Address endpoint;
            private readonly ConcurrentQueue<Connection> connections = new ConcurrentQueue<Connection>();

            public ConnectionEntry(Address endpoint)
            {
                this.endpoint = endpoint;
            }

            public Address Endpoint => endpoint;

            public ConcurrentQueue<Connection> Connections => connections;
        }
    }
}
