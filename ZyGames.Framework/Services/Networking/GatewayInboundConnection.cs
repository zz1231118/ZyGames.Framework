using Framework.Injection;
using Framework.Net.Sockets;

namespace ZyGames.Framework.Services.Networking
{
    internal class GatewayInboundConnection : InboundConnection
    {
        public GatewayInboundConnection(IContainer container, ConnectionListener connectionListener, ExSocket socket)
            : base(container, connectionListener, socket)
        { }
    }
}