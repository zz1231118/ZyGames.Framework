using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicPipeBinding : Binding
    {
        public override ConnectionListener CreateConnectionListener(IServiceProvider serviceProvider)
        {
            return new PipeConnectionListener(serviceProvider);
        }

        public override ClientRuntime CreateClientRuntime(IServiceProvider serviceProvider)
        {
            return new PipeClientRuntime(serviceProvider);
        }

        class PipeChannel : IDisposable
        {
            private const int NoneSentinel = 0;
            private const int SendSentinel = 1;

            private readonly bool isBlocking;
            private readonly int bufferSize;
            private readonly PipeAsyncEventArgs senderAsyncEventArgs;
            private readonly PipeAsyncEventArgs receiverAsyncEventArgs;
            private readonly Queue<byte[]> sendQueue = new Queue<byte[]>();
            private bool isDisposed;
            private PipeStream ioStream;
            private int isInSending;

            public PipeChannel(PipeStream ioStream, bool isBlocking, int bufferSize = 1024)
            {
                this.ioStream = ioStream;
                this.isBlocking = isBlocking;
                this.bufferSize = bufferSize;

                var buffer = new byte[bufferSize];
                senderAsyncEventArgs = new PipeAsyncEventArgs();
                senderAsyncEventArgs.AcceptStream = ioStream;
                senderAsyncEventArgs.Buffer = buffer;
                senderAsyncEventArgs.Offset = 0;
                senderAsyncEventArgs.Count = buffer.Length;
                senderAsyncEventArgs.Completed = new AsyncCallback(IO_Completed);

                buffer = new byte[bufferSize];
                receiverAsyncEventArgs = new PipeAsyncEventArgs();
                receiverAsyncEventArgs.AcceptStream = ioStream;
                receiverAsyncEventArgs.Buffer = buffer;
                receiverAsyncEventArgs.Offset = 0;
                receiverAsyncEventArgs.Count = buffer.Length;
                receiverAsyncEventArgs.Completed = new AsyncCallback(IO_Completed);
            }

            public event EventHandler<PipeChannelEventArgs> DataReceived;

            public event EventHandler<PipeChannelEventArgs> Disconnected;

            private void IO_Completed(IAsyncResult ar)
            {
                var ioEventArgs = (PipeAsyncEventArgs)ar.AsyncState;

                try
                {
                    switch (ioEventArgs.LastOperation)
                    {
                        case PipeAsyncOperation.Send:
                            {
                                ioEventArgs.AcceptStream.EndWrite(ar);
                                ioEventArgs.BytesTransferred = ioEventArgs.Count;
                                ProcessSend(ioEventArgs);
                            }
                            break;
                        case PipeAsyncOperation.Receive:
                            {
                                var bytesTransferred = ioEventArgs.AcceptStream.EndRead(ar);
                                ioEventArgs.BytesTransferred = bytesTransferred;
                                ProcessReceive(ioEventArgs);
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    Closed(ioEventArgs);
                }
            }

            private void PostReceive(PipeAsyncEventArgs ioEventArgs)
            {
                ioEventArgs.LastOperation = PipeAsyncOperation.Receive;
                ioEventArgs.AcceptStream.BeginRead(ioEventArgs.Buffer, ioEventArgs.Offset, ioEventArgs.Count, ioEventArgs.Completed, ioEventArgs);
            }

            private void ProcessReceive(PipeAsyncEventArgs ioEventArgs)
            {
                var length = ioEventArgs.BytesTransferred;
                if (length <= 0)
                {
                    Closing(ioEventArgs);
                    return;
                }

                int copyedBytes;
                int offset = ioEventArgs.Offset;

                do
                {
                    if (!ioEventArgs.IsPrefixReady)
                    {
                        copyedBytes = Math.Min(length, ioEventArgs.prefixBytesLength - ioEventArgs.prefixBytesDone);
                        Array.Copy(ioEventArgs.Buffer, offset, ioEventArgs.byteArrayForPrefix, ioEventArgs.prefixBytesDone, copyedBytes);

                        offset += copyedBytes;
                        length -= copyedBytes;
                        ioEventArgs.prefixBytesDone += copyedBytes;
                        if (ioEventArgs.IsPrefixReady)
                        {
                            ioEventArgs.messageBytesLength = BitConverter.ToInt32(ioEventArgs.byteArrayForPrefix, 0);
                            ioEventArgs.byteArrayForMessage = new byte[ioEventArgs.messageBytesLength];
                        }
                        if (length == 0)
                        {
                            //没有数据了
                            break;
                        }
                    }

                    copyedBytes = Math.Min(length, ioEventArgs.messageBytesLength - ioEventArgs.messageBytesDone);
                    Array.Copy(ioEventArgs.Buffer, offset, ioEventArgs.byteArrayForMessage, ioEventArgs.messageBytesDone, copyedBytes);

                    offset += copyedBytes;
                    length -= copyedBytes;
                    ioEventArgs.messageBytesDone += copyedBytes;
                    if (ioEventArgs.IsMessageReady)
                    {
                        var bytes = ioEventArgs.byteArrayForMessage;
                        DataReceived?.Invoke(this, new PipeChannelEventArgs(bytes));

                        ioEventArgs.Reset();
                    }
                } while (length > 0);

                PostReceive(ioEventArgs);
            }

            private void PostSend(PipeAsyncEventArgs ioEventArgs)
            {
                var copyedBytes = Math.Min(bufferSize, ioEventArgs.messageBytesLength - ioEventArgs.messageBytesDone);
                Array.Copy(ioEventArgs.byteArrayForMessage, ioEventArgs.messageBytesDone, ioEventArgs.Buffer, ioEventArgs.Offset, copyedBytes);
                ioEventArgs.Count = copyedBytes;
                ioEventArgs.LastOperation = PipeAsyncOperation.Send;
                ioEventArgs.AcceptStream.BeginWrite(ioEventArgs.Buffer, ioEventArgs.Offset, ioEventArgs.Count, ioEventArgs.Completed, ioEventArgs);
            }

            private void ProcessSend(PipeAsyncEventArgs ioEventArgs)
            {
                ioEventArgs.messageBytesDone += ioEventArgs.BytesTransferred;
                if (ioEventArgs.messageBytesDone < ioEventArgs.messageBytesLength)
                {
                    PostSend(ioEventArgs);
                }
                else
                {
                    TryDequeueAndPostSend(ioEventArgs);
                }
            }

            private bool DirectSendOrEnqueue(byte[] data)
            {
                lock (this)
                {
                    sendQueue.Enqueue(data);
                    return Interlocked.CompareExchange(ref isInSending, SendSentinel, NoneSentinel) == NoneSentinel;
                }
            }

            private bool TryDequeueOrReset(out byte[] data)
            {
                lock (sendQueue)
                {
                    if (sendQueue.Count > 0)
                    {
                        data = sendQueue.Dequeue();
                        return true;
                    }

                    data = null;
                    Interlocked.Exchange(ref isInSending, NoneSentinel);
                    return false;
                }
            }

            private void TryDequeueAndPostSend(PipeAsyncEventArgs ioEventArgs)
            {
                if (TryDequeueOrReset(out byte[] data))
                {
                    ioEventArgs.byteArrayForMessage = data;
                    ioEventArgs.messageBytesLength = data.Length;
                    ioEventArgs.messageBytesDone = 0;

                    PostSend(ioEventArgs);
                }
            }

            private void Closing(PipeAsyncEventArgs ioEventArgs)
            {
                Closed(ioEventArgs);
            }

            private void Closed(PipeAsyncEventArgs ioEventArgs)
            {
                Disconnected?.Invoke(this, PipeChannelEventArgs.Empty);
            }

            private void Dispose(bool disposing)
            {
                if (!isDisposed)
                {
                    try
                    {
                        DataReceived = null;
                        Disconnected = null;

                        var currentIOStream = ioStream;
                        if (currentIOStream != null)
                        {
                            ioStream = null;
                            currentIOStream.Dispose();
                        }
                    }
                    finally
                    {
                        isDisposed = true;
                    }
                }
            }

            public void Send(byte[] data, int offset, int count)
            {
                var byteArrayForMessageLength = BitConverter.GetBytes(count);
                var byteArrayForPackage = new byte[byteArrayForMessageLength.Length + count];
                Array.Copy(byteArrayForMessageLength, 0, byteArrayForPackage, 0, byteArrayForMessageLength.Length);
                Array.Copy(data, offset, byteArrayForPackage, byteArrayForMessageLength.Length, count);
                if (isBlocking)
                {
                    lock (this)
                    {
                        ioStream.Write(byteArrayForPackage, 0, byteArrayForPackage.Length);
                        ioStream.Flush();
                    }
                }
                else
                {
                    if (DirectSendOrEnqueue(byteArrayForPackage))
                    {
                        TryDequeueAndPostSend(senderAsyncEventArgs);
                    }
                }
            }

            public void Start()
            {
                PostReceive(receiverAsyncEventArgs);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            class PipeAsyncEventArgs
            {
                public PipeStream AcceptStream;
                public PipeAsyncOperation LastOperation;
                public byte[] Buffer;
                public int Offset;
                public int Count;
                public int BytesTransferred;
                public AsyncCallback Completed;

                public byte[] byteArrayForPrefix = new byte[sizeof(int)];
                public byte[] byteArrayForMessage;
                public int prefixBytesDone;
                public int prefixBytesLength = sizeof(int);
                public int messageBytesDone;
                public int messageBytesLength;
                public bool IsPrefixReady => prefixBytesDone == prefixBytesLength;
                public bool IsMessageReady => messageBytesDone == messageBytesLength;

                public void Reset()
                {
                    prefixBytesDone = 0;
                    Array.Clear(byteArrayForPrefix, 0, byteArrayForPrefix.Length);

                    messageBytesLength = 0;
                    messageBytesDone = 0;
                    byteArrayForMessage = null;
                }
            }

            enum PipeAsyncOperation : byte
            {
                Accept,
                Send,
                Receive,
            }

            enum PipeStreamError : byte
            {
                Success,
            }
        }

        class PipeChannelEventArgs : EventArgs
        {
            public static new readonly PipeChannelEventArgs Empty = new PipeChannelEventArgs();

            public PipeChannelEventArgs()
            { }

            public PipeChannelEventArgs(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }

        class PipeConnection : Connection
        {
            private readonly MessageSerializer serializer;
            private readonly MessageDispatcher dispatcher;
            private readonly PipeChannel channel;

            public PipeConnection(MessageSerializer serializer, MessageDispatcher dispatcher, PipeStream ioStream)
            {
                this.serializer = serializer;
                this.dispatcher = dispatcher;
                this.channel = new PipeChannel(ioStream, false);
                this.channel.DataReceived += new EventHandler<PipeChannelEventArgs>(Channel_DataReceived);
                this.channel.Disconnected += new EventHandler<PipeChannelEventArgs>(Channel_Disconnected);
            }

            public Guid Guid { get; } = Guid.NewGuid();

            public event EventHandler Disconnected;

            private void Channel_DataReceived(object sender, PipeChannelEventArgs e)
            {
                var message = serializer.Deserialize(e.Data);
                dispatcher.Dispatch(this, message);
            }

            private void Channel_Disconnected(object sender, PipeChannelEventArgs e)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }

            public override void SendMessage(Message message)
            {
                var bytes = serializer.Serialize(message);
                channel.Send(bytes, 0, bytes.Length);
            }

            public void Start()
            {
                channel.Start();
            }

            public void Close()
            {
                channel.Dispose();
            }
        }

        class PipeConnectionListener : ConnectionListener
        {
            private readonly MessageSerializer serializer;
            private readonly MessageDispatcher dispatcher;
            private readonly BasicPipeBindingOptions bindingOptions;
            private readonly ConcurrentDictionary<Guid, PipeConnection> connections = new ConcurrentDictionary<Guid, PipeConnection>();
            private NamedPipeServerStream pipeServerStream;

            public PipeConnectionListener(IServiceProvider serviceProvider)
            {
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
                this.dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();
                this.bindingOptions = serviceProvider.GetRequiredService<BasicPipeBindingOptions>();
            }

            private void Accetp_Completed(IAsyncResult ar)
            {
                var pipeServerStream = (NamedPipeServerStream)ar.AsyncState;
                try
                {
                    pipeServerStream.EndWaitForConnection(ar);
                }
                catch (Exception)
                {
                    pipeServerStream.Dispose();
                    return;
                }

                ProcessAccept(pipeServerStream);
            }

            private void Connection_Disconnected(object sender, EventArgs e)
            {
                var connection = (PipeConnection)sender;
                connections.TryRemove(connection.Guid, out _);
            }

            private void PostAccept()
            {
                var maxNumberOfServerInstances = bindingOptions.MaxAllowedServerInstances;
                var newPipeServerStream = new NamedPipeServerStream(bindingOptions.PipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeServerStream = newPipeServerStream;

                newPipeServerStream.BeginWaitForConnection(new AsyncCallback(Accetp_Completed), newPipeServerStream);
            }

            private void ProcessAccept(NamedPipeServerStream ioStream)
            {
                PostAccept();

                var connection = new PipeConnection(serializer, dispatcher, ioStream);
                connection.Disconnected += new EventHandler(Connection_Disconnected);
                connections[connection.Guid] = connection;
                connection.Start();
            }

            protected override void OnStart()
            {
                PostAccept();
            }

            protected override void OnStop()
            {
                var oldPipeServerStream = pipeServerStream;
                if (oldPipeServerStream != null)
                {
                    pipeServerStream = null;
                    oldPipeServerStream.Close();
                    oldPipeServerStream.Dispose();
                }

                foreach (var connection in connections.Values)
                {
                    connection.Close();
                }

                connections.Clear();
            }
        }

        class PipeClientRuntime : ClientRuntime
        {
            private readonly MessageSerializer serializer;
            private readonly BasicPipeBindingOptions bindingOption;
            private PipeChannel channel;

            public PipeClientRuntime(IServiceProvider serviceProvider)
            {
                this.serializer = serviceProvider.GetRequiredService<MessageSerializer>();
                this.bindingOption = serviceProvider.GetRequiredService<BasicPipeBindingOptions>();
            }

            private void Channel_DataReceived(object sender, PipeChannelEventArgs e)
            {
                var message = serializer.Deserialize(e.Data);
                Dispatch(message);
            }

            private void Channel_Disconnected(object sender, PipeChannelEventArgs e)
            {
                var currentChannel = Interlocked.Exchange(ref channel, null);
                if (currentChannel != null)
                {
                    currentChannel.Dispose();
                }
            }

            private PipeChannel GetAvailableChannel()
            {
                var currentChannel = channel;
                if (currentChannel == null)
                {
                    lock (this)
                    {
                        if (channel == null)
                        {
                            var clientPipeStream = new NamedPipeClientStream(bindingOption.ServiceName, bindingOption.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                            try
                            {
                                clientPipeStream.Connect();
                            }
                            catch
                            {
                                clientPipeStream.Dispose();
                                throw;
                            }

                            var newChannel = new PipeChannel(clientPipeStream, true);
                            newChannel.DataReceived += new EventHandler<PipeChannelEventArgs>(Channel_DataReceived);
                            newChannel.Disconnected += new EventHandler<PipeChannelEventArgs>(Channel_Disconnected);
                            newChannel.Start();
                            channel = newChannel;
                        }

                        currentChannel = channel;
                    }
                }
                return currentChannel;
            }

            protected override void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    try
                    {
                        var currentChannel = channel;
                        if (currentChannel != null)
                        {
                            channel = null;
                            currentChannel.Dispose();
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }

            public override void SendMessage(Message message)
            {
                var channel = GetAvailableChannel();
                var bytes = serializer.Serialize(message);
                channel.Send(bytes, 0, bytes.Length);
            }
        }
    }
}
