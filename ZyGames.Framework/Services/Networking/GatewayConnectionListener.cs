using System;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services.Networking
{
    internal class GatewayConnectionListener : ConnectionListener
    {
        public GatewayConnectionListener(IServiceProvider serviceProvider, ConnectionListenerOptions connectionListenerOptions)
            : base(serviceProvider, connectionListenerOptions)
        { }

        protected sealed override InboundConnection CreateInboundConnection(ExSocket socket)
        {
            return new GatewayInboundConnection(ServiceProvider, this, socket);
        }
    }
}
