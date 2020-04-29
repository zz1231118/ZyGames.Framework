using System;
using Framework.Net.Sockets;

namespace ZyGames.Framework.Services.Networking
{
    internal class GatewayInboundConnection : InboundConnection
    {
        public GatewayInboundConnection(IServiceProvider serviceProvider, ConnectionListener connectionListener, ExSocket socket)
            : base(serviceProvider, connectionListener, socket)
        { }
    }
}