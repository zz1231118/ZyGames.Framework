using System;
using Framework.Net.Sockets;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services.Networking
{
    internal class ClusterConnectionListener : ConnectionListener
    {
        private readonly ConnectionManager connectionManager;

        public ClusterConnectionListener(IServiceProvider serviceProvider, ConnectionListenerOptions connectionListenerOptions)
            : base(serviceProvider, connectionListenerOptions)
        {
            connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();
        }

        public event EventHandler<ClusterConnectionEventArgs> Terminated;

        protected sealed override InboundConnection CreateInboundConnection(ExSocket socket)
        {
            return new ClusterInbounConnection(ServiceProvider, this, socket, connectionManager);
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
