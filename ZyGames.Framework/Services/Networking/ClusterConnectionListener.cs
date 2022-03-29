using System;
using Framework.Injection;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services.Networking
{
    internal class ClusterConnectionListener : ConnectionListener
    {
        private readonly IConnectionManager connectionManager;

        public ClusterConnectionListener(IContainer container, ConnectionListenerOptions connectionListenerOptions)
            : base(container, connectionListenerOptions)
        {
            connectionManager = container.Required<IConnectionManager>();
        }

        public event EventHandler<ClusterConnectionEventArgs> Terminated;

        protected sealed override InboundConnection CreateInboundConnection(ExSocket socket)
        {
            return new ClusterInbounConnection(Container, this, socket, connectionManager);
        }

        public void ConnectionTerminated(ClusterInbounConnection connection)
        {
            Terminated?.Invoke(this, new ClusterConnectionEventArgs(connection));
        }
    }

    internal class ClusterConnectionEventArgs : EventArgs
    {
        public ClusterConnectionEventArgs(ClusterInbounConnection connection)
        {
            Connection = connection;
        }

        public ClusterInbounConnection Connection { get; }
    }
}
