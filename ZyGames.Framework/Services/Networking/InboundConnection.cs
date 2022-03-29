using System;
using Framework.Injection;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class InboundConnection : Connection
    {
        private readonly ConnectionListener connectionListener;
        private readonly ExSocket socket;

        public InboundConnection(IContainer container, ConnectionListener connectionListener, ExSocket socket)
            : base(container)
        {
            this.connectionListener = connectionListener;
            this.socket = socket;
        }

        public sealed override bool IsConnected => socket?.Connected == true;

        public Guid Guid => socket.Guid;

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
