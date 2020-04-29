using System;

namespace ZyGames.Framework.Remote
{
    internal class ServiceTypeMetadata
    {
        internal ServiceTypeMetadata(Type serviceType, Type interfaceType, Type invokerType)
        {
            ServiceType = serviceType;
            InterfaceType = interfaceType;
            InvokerType = invokerType;
        }

        public Type ServiceType { get; }

        public Type InterfaceType { get; }

        public Type InvokerType { get; }
    }
}
