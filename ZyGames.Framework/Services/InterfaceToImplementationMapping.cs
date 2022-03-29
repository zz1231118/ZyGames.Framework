using System;
using System.Collections.Generic;
using System.Reflection;
using ZyGames.Framework.Services.Collections;
using ZyGames.Framework.Services.Runtime.Generation;

namespace ZyGames.Framework.Services
{
    internal sealed class InterfaceToImplementationMapping
    {
        private readonly CachedReadConcurrentDictionary<Type, Dictionary<Type, Dictionary<int, Entry>>> mappings = new CachedReadConcurrentDictionary<Type, Dictionary<Type, Dictionary<int, Entry>>>();

        private static Dictionary<Type, Dictionary<int, Entry>> CreateMapForConstructedGeneric(Type implementationType)
        {
            var genericType = implementationType.GetGenericTypeDefinition();
            var genericInterfaces = genericType.GetInterfaces();
            var concreteInterfaces = implementationType.GetInterfaces();

            var resultl = new Dictionary<Type, Dictionary<int, Entry>>();
            var genericTypeInfo = genericType.GetTypeInfo();
            var implementationTypeInfo = implementationType.GetTypeInfo();
            for (int i = 0; i < genericInterfaces.Length; i++)
            {
                var genericMethods = ServiceInterfaceUtils.GetMethods(genericInterfaces[i]);
                var concreteInterfaceMethods = ServiceInterfaceUtils.GetMethods(concreteInterfaces[i]);

                var methodMap = new Dictionary<int, Entry>(genericMethods.Length);
                var genericMap = default(InterfaceMapping);
                var concreteMap = default(InterfaceMapping);
                for (int j = 0; j < genericMethods.Length; j++)
                {
                    var genericInterfaceMethod = genericMethods[j];
                    if (genericMap.InterfaceType != genericInterfaceMethod.DeclaringType)
                    {
                        genericMap = genericTypeInfo.GetRuntimeInterfaceMap(genericInterfaceMethod.DeclaringType);
                        concreteMap = implementationTypeInfo.GetRuntimeInterfaceMap(concreteInterfaceMethods[j].DeclaringType);
                    }
                    for (int k = 0; k < genericMap.InterfaceMethods.Length; k++)
                    {
                        if (genericMap.InterfaceMethods[k] != genericInterfaceMethod) continue;

                        methodMap[ServiceInterfaceUtils.ComputeMethodId(genericInterfaceMethod)] = new Entry(concreteMap.TargetMethods[k], concreteMap.InterfaceMethods[k]);
                        break;
                    }
                }

                resultl[concreteInterfaces[i]] = methodMap;
            }

            return resultl;
        }

        private static Dictionary<Type, Dictionary<int, Entry>> CreateMapForNonGeneric(Type implementationType)
        {
            var concreteInterfaces = implementationType.GetInterfaces();
            var interfaces = new List<(Type, Type)>(concreteInterfaces.Length);
            foreach (var iface in concreteInterfaces)
            {
                if (iface.IsConstructedGenericType)
                {
                    interfaces.Add((iface, iface.GetGenericTypeDefinition()));
                }
                else
                {
                    interfaces.Add((iface, null));
                }
            }

            var result = new Dictionary<Type, Dictionary<int, Entry>>();
            var implementationTypeInfo = implementationType.GetTypeInfo();
            foreach (var (iface, genericIface) in interfaces)
            {
                var methods = ServiceInterfaceUtils.GetMethods(iface);
                var genericIfaceMethods = genericIface is not null ? ServiceInterfaceUtils.GetMethods(genericIface) : null;
                var methodMap = new Dictionary<int, Entry>(methods.Length);
                var genericInterfaceMethodMap = genericIface is not null ? new Dictionary<int, Entry>(genericIfaceMethods.Length) : null;
                var mapping = default(InterfaceMapping);
                for (int i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (mapping.InterfaceType != method.DeclaringType)
                    {
                        mapping = implementationTypeInfo.GetRuntimeInterfaceMap(method.DeclaringType);
                    }
                    for (int j = 0; j < mapping.InterfaceMethods.Length; j++)
                    {
                        if (mapping.InterfaceMethods[j] != method) continue;

                        methodMap[ServiceInterfaceUtils.ComputeMethodId(method)] = new Entry(mapping.TargetMethods[j], method);
                        if (genericIface is not null)
                        {
                            var id = ServiceInterfaceUtils.ComputeMethodId(genericIfaceMethods[i]);
                            genericInterfaceMethodMap[id] = new Entry(mapping.TargetMethods[j], genericIfaceMethods[i]);
                            methodMap[id] = new Entry(mapping.TargetMethods[j], method);
                        }

                        break;
                    }
                }

                result[iface] = methodMap;
                if (genericIface is not null)
                {
                    result[genericIface] = genericInterfaceMethodMap;
                }
            }

            return result;
        }

        private static Dictionary<Type, Dictionary<int, Entry>> CreateInterfaceToImplementationMap(Type implementationType)
        {
            var name = implementationType.Name;
            return implementationType.IsConstructedGenericType ? CreateMapForConstructedGeneric(implementationType) : CreateMapForNonGeneric(implementationType);
        }

        public Dictionary<int, Entry> Gain(Type implementationType, Type interfaceType)
        {
            if (!mappings.TryGetValue(implementationType, out var invokerMap))
            {
                mappings[implementationType] = invokerMap = CreateInterfaceToImplementationMap(implementationType);
            }
            if (!invokerMap.TryGetValue(interfaceType, out var interfaceToImplementationMap))
            {
                throw new InvalidOperationException($"Type {implementationType} does not implement interface {interfaceType}");
            }

            return interfaceToImplementationMap;
        }

        public readonly struct Entry
        {
            public readonly MethodInfo ImplementationMethod;
            public readonly MethodInfo InterfaceMethod;

            public Entry(MethodInfo implementationMethod, MethodInfo interfaceMethod)
            {
                ImplementationMethod = implementationMethod;
                InterfaceMethod = interfaceMethod;
            }
        }
    }
}
