using System;
using Framework.Net.Sockets;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal class OutboundConnection : Connection, IDisposable
    {
        private readonly ConnectionManager connectionManager;
        private readonly IMessageSerializer messageSerializer;
        private SlioAddress remoteSlioAddress;
        private TcpClient socket;

        public OutboundConnection(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();
            this.messageSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        }

        public sealed override bool IsConnected => socket?.IsConnected == true;

        private void Socket_Connected(object sender, SocketEventArgs e)
        {
            var socket = (TcpClient)sender;
            socket.DataReceived += new EventHandler<SocketEventArgs>(Socket_DataReceived);
            socket.Disconnected += new EventHandler<SocketEventArgs>(Socket_Disconnected);
        }

        private void Socket_DataReceived(object sender, SocketEventArgs e)
        {
            var message = messageSerializer.Deserialize(e.Data);
            ReceiveMessage(message);
        }

        private void Socket_Disconnected(object sender, SocketEventArgs e)
        {
            var socket = (TcpClient)sender;
            socket.DataReceived -= new EventHandler<SocketEventArgs>(Socket_DataReceived);
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

        public void Connect(SlioAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            remoteSlioAddress = address;
            socket = new TcpClient();
            //socket.SendOperation = SocketOperation.Synchronization;
            socket.Connected += new EventHandler<SocketEventArgs>(Socket_Connected);
            socket.Connect(address.EndPoint);
        }
    }
}
