using System;

namespace ZyGames.Framework.Services.Runtime
{
    public abstract class SystemTargetReference : Reference, ISystemTarget
    {
        protected SystemTargetReference(IServiceProvider serviceProvider, IReferenceRuntime runtime, SlioAddress address, Identity identity)
            : base(serviceProvider, runtime, address, identity)
        { }
    }
}
