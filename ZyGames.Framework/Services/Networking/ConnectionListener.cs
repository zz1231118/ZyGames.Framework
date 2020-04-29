using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using Framework.Net.Sockets;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services.Networking
{
    internal abstract class ConnectionListener
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ConnectionListenerOptions connectionListenerOptions;
        private readonly IMessageSerializer messageSerializer;
        private readonly ConcurrentDictionary<Guid, InboundConnection> connections = new ConcurrentDictionary<Guid, InboundConnection>();
        private SocketListener socketListener;

        public ConnectionListener(IServiceProvider serviceProvider, ConnectionListenerOptions connectionListenerOptions)
        {
            this.serviceProvider = serviceProvider;
            this.connectionListenerOptions = connectionListenerOptions;
            this.messageSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        }

        protected IServiceProvider ServiceProvider => serviceProvider;

        protected ConnectionListenerOptions Options => connectionListenerOptions;

        protected abstract InboundConnection CreateInboundConnection(ExSocket socket);

        private void SocketListener_Connected(object sender, SocketEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var socket = e.Socket;
                var connection = CreateInboundConnection(socket);
                socket.UserToken = connection;
                connections[connection.Guid] = connection;
            }
        }

        private void SocketListener_DataRecieved(object sender, SocketEventArgs e)
        {
            var connection = (Connection)e.Socket.UserToken;
            var message = messageSerializer.Deserialize(e.Data);
            connection.ReceiveMessage(message);
        }

        private void SocketListener_Disconnected(object sender, SocketEventArgs e)
        {
            var connection = (InboundConnection)e.Socket.UserToken;
            connections.TryRemove(connection.Guid, out InboundConnection _);
            connection.Disconnected(e.SocketError != SocketError.Success);
        }

        public void Start()
        {
            var options = connectionListenerOptions;
            var endpoint = options.InsideAddress.EndPoint;
            socketListener = new SocketListener(endpoint, options.Backlog, options.MaxConnections);
            socketListener.Connected += new EventHandler<SocketEventArgs>(SocketListener_Connected);
            socketListener.DataReceived += new EventHandler<SocketEventArgs>(SocketListener_DataRecieved);
            socketListener.Disconnected += new EventHandler<SocketEventArgs>(SocketListener_Disconnected);
            socketListener.Start();
        }

        public void Stop()
        {
            socketListener.Stop();
            socketListener.Dispose();
        }

        public void SendMessage(InboundConnection connection, Message message)
        {
            var bytes = messageSerializer.Serialize(message);
            socketListener.PostSend(connection.Socket, bytes, 0, bytes.Length);
        }
    }
}
