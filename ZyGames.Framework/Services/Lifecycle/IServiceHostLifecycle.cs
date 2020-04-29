using System;
using System.Threading;

namespace ZyGames.Framework.Services.Lifecycle
{
    public interface IServiceHostLifecycle : ILifecycleObservable
    {
        ServiceHostStatus Status { get; }

        CancellationToken Token { get; }

        IDisposable WithStarted(string observerName, Action<CancellationToken> observer);

        IDisposable WithStopped(string observerName, Action<CancellationToken> observer);
    }
}
