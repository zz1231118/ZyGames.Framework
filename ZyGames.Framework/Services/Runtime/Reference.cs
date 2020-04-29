using System;

namespace ZyGames.Framework.Services.Runtime
{
    public abstract class Reference : IAddressable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IReferenceRuntime runtime;
        private readonly SlioAddress address;
        private readonly Identity identity;

        protected Reference(IServiceProvider serviceProvider, IReferenceRuntime runtime, SlioAddress address, Identity identity)
        {
            this.serviceProvider = serviceProvider;
            this.runtime = runtime;
            this.address = address;
            this.identity = identity;
        }

        public abstract int InterfaceId { get; }

        public SlioAddress Address => address;

        public Identity Identity => identity;

        public abstract string GetMethodName(int methodId);

        protected void InvokeMethod(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            runtime.InvokeMethod(this, methodId, arguments, options);
        }

        protected T InvokeMethod<T>(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            return runtime.InvokeMethod<T>(this, methodId, arguments, options);
        }
    }
}
