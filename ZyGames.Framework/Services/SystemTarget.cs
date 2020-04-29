using System;

namespace ZyGames.Framework.Services
{
    public abstract class SystemTarget : Addressable, ISystemTarget
    {
        internal SystemTarget()
        { }

        internal SystemTarget(SlioAddress address, Identity identity)
        {
            Address = address;
            Identity = identity;
        }

        internal abstract Priority Priority { get; }

        public ISystemServiceFactory ServiceFactory { get; internal set; }

        public IServiceProvider ServiceProvider { get; internal set; }

        public sealed override SlioAddress Address { get; internal set; }

        public sealed override Identity Identity { get; internal set; }
    }
}
