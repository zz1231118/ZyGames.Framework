using System;
using System.Threading;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Networking;

namespace ZyGames.Framework.Remote
{
    public sealed class ServiceHost : IDisposable
    {
        private const int NoneSentinel = 0;
        private const int ActiveSentinel = 1;

        private readonly IServiceProvider serviceProvider;
        private readonly Binding binding;
        private int isInActivating;
        private bool isDisposed;
        private ConnectionListener connectionListener;

        internal ServiceHost(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.binding = serviceProvider.GetRequiredService<Binding>();
        }

        public bool IsActivated => isInActivating == ActiveSentinel;

        private void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                try
                {
                    Close();
                }
                finally
                {
                    isDisposed = true;
                }
            }
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref isInActivating, ActiveSentinel, NoneSentinel) != NoneSentinel)
                throw new InvalidOperationException("activated");

            connectionListener = binding.CreateConnectionListener(serviceProvider);
            connectionListener.Start();
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref isInActivating, NoneSentinel, ActiveSentinel) == ActiveSentinel)
            {
                connectionListener.Stop();
                connectionListener.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
