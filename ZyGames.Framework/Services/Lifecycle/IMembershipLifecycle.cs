using System;
using System.Threading;

namespace ZyGames.Framework.Services.Lifecycle
{
    public interface IMembershipLifecycle : ILifecycleObservable
    {
        IDisposable WithChanged(string observerName, Action<CancellationToken> observer);
    }
}
