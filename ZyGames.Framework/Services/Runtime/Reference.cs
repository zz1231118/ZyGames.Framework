namespace ZyGames.Framework.Services.Runtime
{
    public abstract class Reference : IAddressable
    {
        private readonly IReferenceRuntime runtime;
        private readonly Address address;
        private readonly Identity identity;

        protected Reference(IReferenceRuntime runtime, Address address, Identity identity)
        {
            this.runtime = runtime;
            this.address = address;
            this.identity = identity;
        }

        public abstract int InterfaceId { get; }

        public Address Address => address;

        public Identity Identity => identity;

        public abstract string GetMethodName(int methodId);

        protected void InvokeMethod(int methodId, object[] arguments, InvokeMethodOptions options, int timeoutMills)
        {
            runtime.InvokeMethod(this, methodId, arguments, options, timeoutMills);
        }

        protected T InvokeMethod<T>(int methodId, object[] arguments, InvokeMethodOptions options, int timeoutMills)
        {
            return runtime.InvokeMethod<T>(this, methodId, arguments, options, timeoutMills);
        }
    }
}
