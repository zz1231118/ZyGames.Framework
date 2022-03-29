using Framework.Injection;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services.Networking
{
    internal class GatewayConnectionListener : ConnectionListener
    {
        public GatewayConnectionListener(IContainer container, ConnectionListenerOptions connectionListenerOptions)
            : base(container, connectionListenerOptions)
        { }

        protected sealed override InboundConnection CreateInboundConnection(ExSocket socket)
        {
            return new GatewayInboundConnection(Container, this, socket);
        }
    }
}
