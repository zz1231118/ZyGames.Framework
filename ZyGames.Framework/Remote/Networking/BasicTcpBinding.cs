using System;
using System.Collections.Concurrent;
using Framework.Net.Sockets;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicTcpBinding : Binding
    {
        public override ConnectionListener CreateConnectionListener(IServiceProvider serviceProvider)
        {
            return new TcpConnectionListener(serviceProvider);
        }

        public override ClientRuntime CreateClientRuntime(IServiceProvider serviceProvider)
        {
            return new TcpClientRuntime(serviceProvider);
        }

        class TcpConnection : Connection
        {
            private readonly SocketListener socketListener;
            private readonly ExSocket socket;
            private readonly MessageSerializer serializer;

            public TcpConnection(SocketListener socketListener, ExSocket socket, MessageSerializer serializer)
            {
                this.socketListener = socketListener;
                this.socket = socket;
                this.serializer = serializer;
            }

            public Guid Guid => socket.Guid;

            public override void SendMessage(Message message)
            {
                var bytes = serializer.Serialize(message);
                socketListener.Send(socket, bytes);
            }

            public void Close()
            {
                socket.Close();
            }
        }

        class TcpConnectionListener : ConnectionListener
        {
            private readonly MessageSerializer serializer;
            private readonly MessageDispatcher dispatcher;
            private readonly BasicTcpBindingOptions options;
            private readonly ConcurrentDictionary<Guid, TcpConnection> connections = new ConcurrentDictionary<Guid, TcpConnection>();
            private SocketListener socketListener;

            public TcpConnectionListener(IServiceProvider serviceProvider)
            {
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
                this.dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();
                this.options = serviceProvider.GetRequiredService<BasicTcpBindingOptions>();
            }

            private void SocketListener_Connected(object sender, SocketEventArgs e)
            {
                if (e.SocketError == System.Net.Sockets.SocketError.Success)
                {
                    var connection = new TcpConnection(socketListener, e.Socket, serializer);
                    e.Socket.UserToken = connection;
                    connections[connection.Guid] = connection;
                }
            }

            private void SocketListener_DataReceived(object sender, SocketEventArgs e)
            {
                var connection = (TcpConnection)e.Socket.UserToken;
                var message = serializer.Deserialize(e.Data);
                dispatcher.Dispatch(connection, message);
            }

            private void SocketListener_Disconnected(object sender, SocketEventArgs e)
            {
                connections.TryRemove(e.Socket.Guid, out _);
            }

            protected override void OnStart()
            {
                socketListener = new SocketListener(options.EndPoint, options.Backlog, options.MaxConnections);
                socketListener.Connected += new EventHandler<SocketEventArgs>(SocketListener_Connected);
                socketListener.DataReceived += new EventHandler<SocketEventArgs>(SocketListener_DataReceived);
                socketListener.Disconnected += new EventHandler<SocketEventArgs>(SocketListener_Disconnected);
                socketListener.Start();
            }

            protected override void OnStop()
            {
                socketListener.Stop();
                socketListener.Dispose();

                foreach (var connection in connections.Values)
                {
                    connection.Close();
                }

                connections.Clear();
            }

            protected override void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    try
                    {
                        socketListener.Dispose();
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }

            public void SendMessage(Connection connection, Message message)
            {
                var bytes = serializer.Serialize(message);
                throw new System.NotImplementedException();
            }
        }

        class TcpClientRuntime : ClientRuntime
        {
            private readonly MessageSerializer serializer;
            private readonly BasicTcpBindingOptions option;
            private TcpClient socketClient;

            public TcpClientRuntime(IServiceProvider serviceProvider)
            {
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
                this.option = serviceProvider.GetRequiredService<BasicTcpBindingOptions>();
            }

            private void SocketClient_Connected(object sender, SocketEventArgs e)
            {
                var newClient = (TcpClient)sender;
                newClient.DataReceived += new EventHandler<SocketEventArgs>(SocketClient_DataReceived);
                newClient.Disconnected += new EventHandler<SocketEventArgs>(SocketClient_Disconnected);
            }

            private void SocketClient_DataReceived(object sender, SocketEventArgs e)
            {
                var message = serializer.Deserialize(e.Data);
                Dispatch(message);
            }

            private void SocketClient_Disconnected(object sender, SocketEventArgs e)
            {
                var newClient = (TcpClient)sender;
                newClient.DataReceived -= new EventHandler<SocketEventArgs>(SocketClient_DataReceived);
                newClient.Disconnected -= new EventHandler<SocketEventArgs>(SocketClient_Disconnected);
            }

            private TcpClient GetAvailableClient()
            {
                var currentClient = socketClient;
                if (currentClient == null || !currentClient.IsConnected)
                {
                    lock (this)
                    {
                        if (socketClient != null && !socketClient.IsConnected)
                        {
                            socketClient.Dispose();
                            socketClient = null;
                        }
                        if (socketClient == null)
                        {
                            var newClient = new TcpClient();
                            newClient.SendOperation = SocketOperation.Synchronization;
                            newClient.Connected += new EventHandler<SocketEventArgs>(SocketClient_Connected);
                            try
                            {
                                newClient.Connect(option.EndPoint);
                            }
                            catch
                            {
                                newClient.Connected -= new EventHandler<SocketEventArgs>(SocketClient_Connected);
                                newClient.Close();
                                newClient.Dispose();
                                throw;
                            }

                            socketClient = newClient;
                        }

                        currentClient = socketClient;
                    }
                }
                return currentClient;
            }

            public override void SendMessage(Message message)
            {
                var currentClient = GetAvailableClient();
                var bytes = serializer.Serialize(message);
                currentClient.Send(bytes);
            }
        }
    }
}