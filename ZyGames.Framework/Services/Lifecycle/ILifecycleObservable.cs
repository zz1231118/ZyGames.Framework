using System;
using System.Threading;

namespace ZyGames.Framework.Services.Lifecycle
{
    public interface ILifecycleObservable
    {
        IDisposable Subscribe(string observerName, int stage, ILifecycleObserver observer);

        IDisposable Subscribe(string observerName, int stage, Action<CancellationToken, int> observer);

        void NotifyObserver(CancellationToken token, int state);

        void NotifyObserver(int state);
    }
}
