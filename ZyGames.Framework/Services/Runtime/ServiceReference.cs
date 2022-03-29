using Framework.Injection;
using ZyGames.Framework.Services.Membership;

namespace ZyGames.Framework.Services.Runtime
{
    public abstract class ServiceReference : Reference, IService
    {
        private readonly MembershipManager membershipManager;
        private readonly object metadata;

        protected ServiceReference(IContainer container, IReferenceRuntime runtime, Address address, Identity identity, object metadata)
            : base(runtime, address, identity)
        {
            this.membershipManager = container.Required<MembershipManager>();
            this.metadata = metadata;
        }

        public bool IsAlived => membershipManager.Alive(Identity);

        public object Metadata => metadata;

        public T GetMeta<T>()
            where T : class
        {
            return metadata as T;
        }

        public T GetMetadata<T>()
            where T : class
        {
            return metadata as T;
        }
    }
}