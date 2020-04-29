using System;

namespace ZyGames.Framework.Remote.Networking
{
    public abstract class ConnectionListener : IDisposable
    {
        private bool isActivated;
        private bool isDisposed;

        public bool IsActivated => isActivated;

        protected bool IsDisposed => isDisposed;

        protected abstract void OnStart();

        protected abstract void OnStop();

        protected void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                try
                {
                    Stop();
                }
                finally
                {
                    isDisposed = true;
                }
            }
        }

        public void Start()
        {
            CheckDisposed();

            if (isActivated)
                throw new InvalidOperationException("activated");

            OnStart();
            isActivated = true;
        }

        public void Stop()
        {
            if (isActivated)
            {
                try
                {
                    OnStop();
                }
                finally
                {
                    isActivated = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
