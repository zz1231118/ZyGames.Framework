using System;
using Framework.Injection;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Networking
{
    internal abstract class Connection : IDisposable
    {
        private readonly MessageCenter messageCenter;
        private bool isDisposed;

        public Connection(IContainer container)
        {
            this.messageCenter = container.Required<MessageCenter>();
        }

        protected bool IsDisposed => isDisposed;

        public abstract bool IsConnected { get; }

        protected void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            isDisposed = true;
        }

        public abstract void SendMessage(Message message);

        public abstract void Disconnected(bool forcible);

        public virtual void ReceiveMessage(Message message)
        {
            messageCenter.ReceiveMessage(message);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
