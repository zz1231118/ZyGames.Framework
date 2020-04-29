using System;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class ClusterInbounConnection : InboundConnection
    {
        private readonly ClusterConnectionListener connectionListener;
        private readonly ConnectionManager connectionManager;
        private SlioAddress localSlioAddress;
        private SlioAddress remoteSlioAddress;
        private bool handshaked;

        public ClusterInbounConnection(IServiceProvider serviceProvider, ClusterConnectionListener connectionListener, ExSocket socket, ConnectionManager connectionManager)
            : base(serviceProvider, connectionListener, socket)
        {
            this.connectionListener = connectionListener;
            this.connectionManager = connectionManager;
        }

        public bool Handshaked => handshaked;

        public SlioAddress LocalSlioAddress => localSlioAddress;

        public SlioAddress RemoteSlioAddress => remoteSlioAddress;

        public override void ReceiveMessage(Message message)
        {
            if (!handshaked)
            {
                localSlioAddress = message.TargetSlio;
                remoteSlioAddress = message.SendingSlio;
                connectionManager.Connected(remoteSlioAddress, this);
                handshaked = true;
            }

            base.ReceiveMessage(message);
        }

        public override void Disconnected(bool forcible)
        {
            if (handshaked)
            {
                connectionListener.ConnectionTerminated(this);
                connectionManager.ConnectionTerminated(remoteSlioAddress, this);
            }

            base.Disconnected(forcible);
        }
    }
}
