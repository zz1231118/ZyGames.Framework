using System.Threading;

namespace ZyGames.Framework.Services.Lifecycle
{
    public interface ILifecycleObserver
    {
        void Notify(CancellationToken token, int state);
    }
}
