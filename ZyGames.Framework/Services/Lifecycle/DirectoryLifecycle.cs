using Framework.Log;

namespace ZyGames.Framework.Services.Lifecycle
{
    internal class DirectoryLifecycle : LifecycleObservable, IDirectoryLifecycle
    {
        public DirectoryLifecycle(ILogger logger)
            : base(logger)
        { }
    }
}
