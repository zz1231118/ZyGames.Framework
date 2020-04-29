using System;
using System.Threading;

namespace ZyGames.Framework.Services.Messaging
{
    internal class CompletionSource<T> : IDisposable
    {
        private const int NoneSentinel = 0;
        private const int CompletedSentinel = 1;
        private readonly ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim();
        private bool isDisposed;
        private volatile int isInCompleted;
        private volatile Exception exception;
        private volatile object result;

        public bool IsCompleted => isInCompleted == CompletedSentinel;

        public bool IsFaulted => exception != null;

        protected void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                manualResetEventSlim.Dispose();
            }
        }

        public T GetResult(TimeSpan timeout)
        {
            if (!manualResetEventSlim.Wait(timeout))
                throw new TimeoutException();
            if (IsFaulted)
                throw exception;

            return (T)result;
        }

        public T GetResult()
        {
            return GetResult(Timeout.InfiniteTimeSpan);
        }

        public void SetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (Interlocked.CompareExchange(ref isInCompleted, CompletedSentinel, NoneSentinel) == NoneSentinel)
            {
                this.exception = exception;
                manualResetEventSlim.Set();
            }
        }

        public void SetResult(T result)
        {
            if (Interlocked.CompareExchange(ref isInCompleted, CompletedSentinel, NoneSentinel) == NoneSentinel)
            {
                this.result = result;
                manualResetEventSlim.Set();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
