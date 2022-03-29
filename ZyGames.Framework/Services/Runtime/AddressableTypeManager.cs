using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ZyGames.Framework.Services.Runtime.Generation;

namespace ZyGames.Framework.Services.Runtime
{
    internal class AddressableTypeManager
    {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private readonly Dictionary<Type, AddressableTypeData> addressTypes = new Dictionary<Type, AddressableTypeData>();

        public AddressableTypeManager()
        {
            var assemblyName = new AssemblyName("DynamicAddressableAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        private AddressableTypeData GetAddressableTypeData(Type baseType, Type interfaceType)
        {
            lock (addressTypes)
            {
                if (!addressTypes.TryGetValue(interfaceType, out AddressableTypeData addressTypeData))
                {
                    var methods = ServiceInterfaceUtils.GetMethods(interfaceType);
                    var referenceTypeGenerator = new ReferenceTypeGenerator(moduleBuilder, interfaceType, methods, baseType);
                    var methodInvokerTypeGenerator = new InvokerTypeGenerator(moduleBuilder, interfaceType, methods);
                    var referenceType = referenceTypeGenerator.GenerateType();
                    var invokerType = methodInvokerTypeGenerator.GenerateType();
                    addressTypeData = new AddressableTypeData(interfaceType, referenceType, invokerType);
                    addressTypes[interfaceType] = addressTypeData;
                }

                return addressTypeData;
            }
        }

        public AddressableTypeData GetSystemTargetTypeData(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            return GetAddressableTypeData(typeof(SystemTargetReference), interfaceType);
        }

        public AddressableTypeData GetServiceTypeData(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            return GetAddressableTypeData(typeof(ServiceReference), interfaceType);
        }
    }
}
