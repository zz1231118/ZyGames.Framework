using Framework.Injection;

namespace ZyGames.Framework.Services
{
    public abstract class SystemTarget : Addressable, ISystemTarget
    {
        internal SystemTarget()
        { }

        internal SystemTarget(Address address, Identity identity)
        {
            Address = address;
            Identity = identity;
        }

        public IContainer Container { get; internal set; }

        public ISystemServiceFactory ServiceFactory { get; internal set; }

        public sealed override Address Address { get; internal set; }

        public sealed override Identity Identity { get; internal set; }
    }
}
