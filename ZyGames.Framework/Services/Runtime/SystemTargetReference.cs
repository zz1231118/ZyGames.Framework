namespace ZyGames.Framework.Services.Runtime
{
    public abstract class SystemTargetReference : Reference, ISystemTarget
    {
        protected SystemTargetReference(IReferenceRuntime runtime, Address address, Identity identity)
            : base(runtime, address, identity)
        { }
    }
}
