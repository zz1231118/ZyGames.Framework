using System;
using Framework.Injection;

namespace ZyGames.Framework.Services.Runtime
{
    internal class AddressableTypeData
    {
        private readonly Type interfaceType;
        private readonly Type referenceType;
        private readonly Type invokerType;

        public AddressableTypeData(Type interfaceType, Type referenceType, Type invokerType)
        {
            this.interfaceType = interfaceType;
            this.referenceType = referenceType;
            this.invokerType = invokerType;
        }

        public Type InterfaceType => interfaceType;

        public Type ReferenceType => referenceType;

        public Type InvokerType => invokerType;

        public ISystemTarget CreateSystemTargetReference(IReferenceRuntime runtime, Address address, Identity identity)
        {
            return (ISystemTarget)Activator.CreateInstance(referenceType, runtime, address, identity);
        }

        public IService CreateServiceReference(IContainer container, IReferenceRuntime runtime, Address address, Identity identity, object metadata)
        {
            return (IService)Activator.CreateInstance(referenceType, container, runtime, address, identity, metadata);
        }

        public IMethodInvoker CreateMethodInvoker()
        {
            return (IMethodInvoker)Activator.CreateInstance(invokerType);
        }
    }
}
