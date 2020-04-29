using System;

namespace ZyGames.Framework.Services.Lifecycle
{
    public class LifecycleCanceledException : Exception
    {
        public LifecycleCanceledException(string message)
            : base(message)
        { }

        public LifecycleCanceledException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
