using System;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Membership;

namespace ZyGames.Framework.Services.Runtime
{
    public abstract class ServiceReference : Reference, IService
    {
        private readonly MembershipManager membershipManager;
        private readonly object metadata;

        protected ServiceReference(IServiceProvider serviceProvider, IReferenceRuntime runtime, SlioAddress address, Identity identity, object metadata)
            : base(serviceProvider, runtime, address, identity)
        {
            this.membershipManager = serviceProvider.GetRequiredService<MembershipManager>();
            this.metadata = metadata;
        }

        public bool IsAlived => membershipManager.Alive(Identity);

        public object Metadata => metadata;

        public T GetMetadata<T>()
        {
            return (T)metadata;
        }
    }
}