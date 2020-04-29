using System;

namespace ZyGames.Framework.Services.Runtime
{
    internal class AddressableTypeData
    {
        public AddressableTypeData(Type interfaceType, Type referenceType, Type invokerType)
        {
            InterfaceType = interfaceType;
            ReferenceType = referenceType;
            InvokerType = invokerType;
        }

        public Type InterfaceType { get; }

        public Type ReferenceType { get; }

        public Type InvokerType { get; }

        public ISystemTarget CreateSystemTargetReference(IServiceProvider serviceProvider, IReferenceRuntime runtime, SlioAddress address, Identity identity)
        {
            return (ISystemTarget)Activator.CreateInstance(ReferenceType, serviceProvider, runtime, address, identity);
        }

        public IService CreateServiceReference(IServiceProvider serviceProvider, IReferenceRuntime runtime, SlioAddress address, Identity identity, object metadata)
        {
            return (IService)Activator.CreateInstance(ReferenceType, serviceProvider, runtime, address, identity, metadata);
        }

        public IMethodInvoker CreateMethodInvoker()
        {
            return (IMethodInvoker)Activator.CreateInstance(InvokerType);
        }
    }
}
