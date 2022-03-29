using System;

namespace ZyGames.Framework.Services.Dashboard.Metrics
{
    public interface IServiceProfiler
    {
        void Track(double elapsedMs, Type serviceType, string methodName, bool failed = false);
    }
}
