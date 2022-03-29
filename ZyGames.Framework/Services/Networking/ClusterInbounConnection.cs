using Framework.Injection;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class ClusterInbounConnection : InboundConnection
    {
        private readonly ClusterConnectionListener connectionListener;
        private readonly IConnectionManager connectionManager;
        private Address localSiloAddress;
        private Address remoteSiloAddress;
        private bool handshaked;

        public ClusterInbounConnection(IContainer container, ClusterConnectionListener connectionListener, ExSocket socket, IConnectionManager connectionManager)
            : base(container, connectionListener, socket)
        {
            this.connectionListener = connectionListener;
            this.connectionManager = connectionManager;
        }

        public bool Handshaked => handshaked;

        public Address LocalSiloAddress => localSiloAddress;

        public Address RemoteSiloAddress => remoteSiloAddress;

        public override void ReceiveMessage(Message message)
        {
            if (!handshaked)
            {
                localSiloAddress = message.TargetSilo;
                remoteSiloAddress = message.SendingSilo;
                connectionManager.Connected(remoteSiloAddress, this);
                handshaked = true;
            }

            base.ReceiveMessage(message);
        }

        public override void Disconnected(bool forcible)
        {
            if (handshaked)
            {
                connectionListener.ConnectionTerminated(this);
                connectionManager.ConnectionTerminated(remoteSiloAddress, this);
            }

            base.Disconnected(forcible);
        }
    }
}
