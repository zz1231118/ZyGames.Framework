using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote
{
    internal class ServiceTypeMetadataManager : ServiceTypeManager
    {
        private readonly Dictionary<Type, ServiceTypeMetadata> serviceTypes = new Dictionary<Type, ServiceTypeMetadata>();

        public IReadOnlyCollection<ServiceTypeMetadata> Metadatas => serviceTypes.Values;

        private void DefineServiceInvokerTypeConstructors(TypeBuilder typeBuilder)
        {
            var parentType = typeof(object);
            var parentConstructor = parentType.GetConstructor(Type.EmptyTypes);
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var iLGenerator = constructorBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, parentConstructor);
            iLGenerator.Emit(OpCodes.Ret);
        }

        private void DefineServiceInvokerTypeBody(TypeBuilder typeBuilder, Type interfaceType)
        {
            var interfaceId = ServiceUtility.GetServiceInterfaceId(interfaceType);
            var interfaceIdGetterMethodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var interfaceIdGetterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(IServiceMethodInvoker.InterfaceId)), interfaceIdGetterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = interfaceIdGetterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            var interfaceIdPropertyBuilder = typeBuilder.DefineProperty(nameof(IServiceMethodInvoker.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            interfaceIdPropertyBuilder.SetGetMethod(interfaceIdGetterMethodBuilder);

            var interfaceMethod = typeof(IServiceMethodInvoker).GetMethod(nameof(IServiceMethodInvoker.Invoke));
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notImplementedExceptionConstructor = typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) });
            var getMethodIdProperty = typeof(MethodInvokeRequest).GetProperty(nameof(MethodInvokeRequest.MethodId));
            var getArgumentsProperty = typeof(MethodInvokeRequest).GetProperty(nameof(MethodInvokeRequest.Arguments));
            var methodParameterTypes = new Type[] { typeof(IService), typeof(MethodInvokeRequest) };
            var methodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var methodBuilder = typeBuilder.DefineMethod(nameof(IServiceMethodInvoker.Invoke), methodAttributes, CallingConventions.HasThis, typeof(object), methodParameterTypes);
            var parameters = interfaceMethod.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            }

            iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.DeclareLocal(typeof(int));
            iLGenerator.DeclareLocal(typeof(object));

            iLGenerator.Emit(OpCodes.Ldarg_2);
            iLGenerator.Emit(OpCodes.Callvirt, getMethodIdProperty.GetMethod);
            iLGenerator.Emit(OpCodes.Stloc_0);
            var bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            var methods = interfaceType.GetMethods(bindingAttr);
            var retLabel = iLGenerator.DefineLabel();
            var defLabel = iLGenerator.DefineLabel();
            var jumpTable = new Label[methods.Length];
            var bodyTable = new Label[methods.Length];
            for (int i = 0; i < jumpTable.Length; i++)
            {
                jumpTable[i] = iLGenerator.DefineLabel();
                bodyTable[i] = iLGenerator.DefineLabel();
            }
            var endSentinel = methods.Length - 1;
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var methodId = ServiceUtility.ComputeMethodId(method);
                iLGenerator.MarkLabel(jumpTable[i]);
                iLGenerator.Emit(OpCodes.Ldloc_0);
                iLGenerator.Emit(OpCodes.Ldc_I4, methodId);
                iLGenerator.Emit(OpCodes.Beq, bodyTable[i]);
                iLGenerator.Emit(OpCodes.Br, i < endSentinel ? jumpTable[i + 1] : defLabel);
            }
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                iLGenerator.MarkLabel(bodyTable[i]);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Castclass, interfaceType);
                parameters = method.GetParameters();
                for (var j = 0; j < parameters.Length; j++)
                {
                    var parameter = parameters[j];
                    iLGenerator.Emit(OpCodes.Ldarg_2);
                    iLGenerator.Emit(OpCodes.Callvirt, getArgumentsProperty.GetMethod);
                    EmitLdc(iLGenerator, j);
                    iLGenerator.Emit(OpCodes.Ldelem_Ref);
                    if (parameter.ParameterType.IsValueType)
                    {
                        iLGenerator.Emit(OpCodes.Unbox_Any, parameter.ParameterType);
                    }
                    else
                    {
                        iLGenerator.Emit(OpCodes.Castclass, parameter.ParameterType);
                    }
                }
                iLGenerator.Emit(OpCodes.Callvirt, method);
                if (method.ReturnType == typeof(void))
                {
                    iLGenerator.Emit(OpCodes.Ldnull);
                }
                else if (method.ReturnType.IsValueType)
                {
                    iLGenerator.Emit(OpCodes.Box, method.ReturnType);
                }

                iLGenerator.Emit(OpCodes.Stloc_1);
                iLGenerator.Emit(OpCodes.Br, retLabel);
            }

            iLGenerator.MarkLabel(defLabel);
            iLGenerator.Emit(OpCodes.Ldstr, "interfaceId={0},methodId={1}");
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, interfaceIdPropertyBuilder.GetMethod);
            iLGenerator.Emit(OpCodes.Box, interfaceIdPropertyBuilder.PropertyType);
            iLGenerator.Emit(OpCodes.Ldarg_2);
            iLGenerator.Emit(OpCodes.Callvirt, getMethodIdProperty.GetMethod);
            iLGenerator.Emit(OpCodes.Box, getMethodIdProperty.PropertyType);
            iLGenerator.Emit(OpCodes.Call, stringFormatMethod);
            iLGenerator.Emit(OpCodes.Newobj, notImplementedExceptionConstructor);
            iLGenerator.Emit(OpCodes.Throw);

            iLGenerator.MarkLabel(retLabel);
            iLGenerator.Emit(OpCodes.Ldloc_1);
            iLGenerator.Emit(OpCodes.Ret);
        }

        protected Type CreateServiceInvokerType(Type interfaceType)
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}ServiceGeneric{1}MethodInvoker", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;
            var typeBuilder = DefineType(typeFullName, typeAttributes, typeof(object), new Type[] { typeof(IServiceMethodInvoker) });
            DefineServiceInvokerTypeConstructors(typeBuilder);
            DefineServiceInvokerTypeBody(typeBuilder, interfaceType);
            return typeBuilder.CreateTypeInfo();
        }

        private ServiceTypeMetadata CreateServiceTypeMetadata(Type serviceType, Type interfaceType)
        {
            var serviceInvokerType = CreateServiceInvokerType(interfaceType);
            return new ServiceTypeMetadata(serviceType, interfaceType, serviceInvokerType);
        }

        public ServiceTypeMetadata AddServiceType(Type serviceType)
        {
            if (!serviceType.IsClass || serviceType.IsInterface || serviceType.IsAbstract)
            {
                throw new ArgumentException("types that are not supported.");
            }
            if (!typeof(IService).IsAssignableFrom(serviceType))
            {
                throw new ArgumentException("the IService interface is not implemented.");
            }

            Type interfaceType = null;
            ServiceContractAttribute serviceContract = null;
            foreach (var type in serviceType.GetInterfaces())
            {
                serviceContract = type.GetCustomAttribute<ServiceContractAttribute>();
                if (serviceContract != null)
                {
                    interfaceType = type;
                    break;
                }
            }
            if (serviceContract == null)
            {
                throw new ArgumentException("not contract interface type.");
            }

            var serviceTypeData = CreateServiceTypeMetadata(serviceType, interfaceType);
            serviceTypes[serviceType] = serviceTypeData;
            return serviceTypeData;
        }

        public ServiceTypeMetadata AddServiceType<T>()
            where T : class
        {
            return AddServiceType(typeof(T));
        }

        public ServiceTypeMetadata GetServiceTypeMetadata(Type serviceType)
        {
            serviceTypes.TryGetValue(serviceType, out ServiceTypeMetadata serviceTypeData);
            return serviceTypeData;
        }
    }
}
