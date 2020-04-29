namespace ZyGames.Framework.Remote
{
    public abstract class ServiceReference
    {
        private readonly ServiceReferenceRuntime runtime;

        protected ServiceReference(ServiceReferenceRuntime runtime)
        {
            this.runtime = runtime;
        }

        public abstract int InterfaceId { get; }

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