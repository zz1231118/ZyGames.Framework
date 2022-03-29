using System;
using Framework.Injection;
using Framework.Net.Sockets;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class OutboundConnection : Connection, IDisposable
    {
        private readonly IConnectionManager connectionManager;
        private readonly IMessageSerializer messageSerializer;
        private Address remoteSlioAddress;
        private TcpClient socket;

        public OutboundConnection(IContainer container)
            : base(container)
        {
            this.connectionManager = container.Required<IConnectionManager>();
            this.messageSerializer = container.Required<IMessageSerializer>();
        }

        public sealed override bool IsConnected => socket?.IsConnected == true;

        private void Socket_Connected(object sender, SocketEventArgs e)
        {
            var socket = (TcpClient)sender;
            socket.Received += new EventHandler<SocketEventArgs>(Socket_Received);
            socket.Disconnected += new EventHandler<SocketEventArgs>(Socket_Disconnected);
        }

        private void Socket_Received(object sender, SocketEventArgs e)
        {
            var message = messageSerializer.Deserialize(e.Data);
            ReceiveMessage(message);
        }

        private void Socket_Disconnected(object sender, SocketEventArgs e)
        {
            var socket = (TcpClient)sender;
            socket.Received -= new EventHandler<SocketEventArgs>(Socket_Received);
            socket.Disconnected -= new EventHandler<SocketEventArgs>(Socket_Disconnected);
            Disconnected(e.SocketError != System.Net.Sockets.SocketError.Success);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                try
                {
                    socket?.Dispose();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override void SendMessage(Message message)
        {
            if (socket == null)
                throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.NotConnected);

            var bytes = messageSerializer.Serialize(message);
            socket.Send(bytes, 0, bytes.Length);
        }

        public override void Disconnected(bool forcible)
        {
            connectionManager.ConnectionTerminated(remoteSlioAddress, this);
        }

        public void Connect(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            remoteSlioAddress = address;
            var endpoint = address.GetEndPoint();

            socket = new TcpClient();
            socket.SendOperation = SocketOperation.Synchronization;
            socket.Connected += new EventHandler<SocketEventArgs>(Socket_Connected);
            socket.Connect(endpoint);
        }
    }
}
