using System;

namespace ZyGames.Framework.Remote
{
    internal class ServiceTypeData
    {
        internal ServiceTypeData(Type interfaceType, Type referenceType, ServiceReferenceCreator referenceCreator)
        {
            InterfaceType = interfaceType;
            ReferenceType = referenceType;
            ReferenceCreator = referenceCreator;
        }

        public Type InterfaceType { get; }

        public Type ReferenceType { get; }

        public ServiceReferenceCreator ReferenceCreator { get; }
    }
}
