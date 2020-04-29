using System;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class InboundConnection : Connection
    {
        private readonly ConnectionListener connectionListener;
        private readonly ExSocket socket;

        public InboundConnection(IServiceProvider serviceProvider, ConnectionListener connectionListener, ExSocket socket)
            : base(serviceProvider)
        {
            this.connectionListener = connectionListener;
            this.socket = socket;
        }

        public sealed override bool IsConnected => socket?.Connected == true;

        public Guid Guid => socket.HashCode;

        public ExSocket Socket => socket;

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                try
                {
                    socket?.Close();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override void Disconnected(bool forcible)
        { }

        public override void SendMessage(Message message)
        {
            connectionListener.SendMessage(this, message);
        }
    }
}
