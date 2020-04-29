using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ZyGames.Framework.Remote
{
    internal class ServiceTypeDataManager : ServiceTypeManager
    {
        private readonly Dictionary<Type, ServiceTypeData> serviceTypes = new Dictionary<Type, ServiceTypeData>();

        private void DefineServiceReferenceTypeConstructors(TypeBuilder typeBuilder)
        {
            var parentType = typeof(ServiceReference);
            var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var parentConstructors = parentType.GetConstructors(bindingAttr);
            foreach (var parentConstructor in parentConstructors)
            {
                var parameters = parentConstructor.GetParameters();
                var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;
                var callingConventions = CallingConventions.Standard | CallingConventions.HasThis;
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                var constructorBuilder = typeBuilder.DefineConstructor(methodAttributes, callingConventions, parameterTypes);
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    constructorBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
                }

                var iLGenerator = constructorBuilder.GetILGenerator();
                iLGenerator.Emit(OpCodes.Ldarg_0);
                for (int i = 1; i <= parameters.Length; i++)
                {
                    EmitLdarg(iLGenerator, i);
                }

                iLGenerator.Emit(OpCodes.Call, parentConstructor);
                iLGenerator.Emit(OpCodes.Ret);
            }
        }

        private void DefineServiceReferenceTypeInterfaceIdProperty(TypeBuilder typeBuilder, Type interfaceType, out PropertyBuilder propertyBuilder)
        {
            var interfaceId = ServiceUtility.GetServiceInterfaceId(interfaceType);
            var getterMethodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
            var getterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(ServiceReference.InterfaceId)), getterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = getterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            propertyBuilder = typeBuilder.DefineProperty(nameof(ServiceReference.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            propertyBuilder.SetGetMethod(getterMethodBuilder);
        }

        private void DefineServiceReferenceTypeGetMethodNameMethod(TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdProperty)
        {
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notImplementedExceptionConstructor = typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) });

            var methodTemplate = typeof(ServiceReference).GetMethod(nameof(ServiceReference.GetMethodName));
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var parameters = methodTemplate.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(methodTemplate.Name, methodAttributes, CallingConventions.Standard, methodTemplate.ReturnType, parameterTypes);
            var bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            var methods = interfaceType.GetMethods(bindingAttr);
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            }

            var iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.DeclareLocal(typeof(object));
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
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Ldc_I4, methodId);
                iLGenerator.Emit(OpCodes.Beq, bodyTable[i]);
                iLGenerator.Emit(OpCodes.Br, i < endSentinel ? jumpTable[i + 1] : defLabel);
            }
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                iLGenerator.MarkLabel(bodyTable[i]);
                iLGenerator.Emit(OpCodes.Ldstr, method.Name);

                iLGenerator.Emit(OpCodes.Stloc_0);
                iLGenerator.Emit(OpCodes.Br, retLabel);
            }

            iLGenerator.MarkLabel(defLabel);
            iLGenerator.Emit(OpCodes.Ldstr, "interfaceId={0},methodId={1}");
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Callvirt, interfaceIdProperty.GetMethod);
            iLGenerator.Emit(OpCodes.Box, interfaceIdProperty.PropertyType);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Box, typeof(int));
            iLGenerator.Emit(OpCodes.Call, stringFormatMethod);
            iLGenerator.Emit(OpCodes.Newobj, notImplementedExceptionConstructor);
            iLGenerator.Emit(OpCodes.Throw);

            iLGenerator.MarkLabel(retLabel);
            iLGenerator.Emit(OpCodes.Ldloc_0);
            iLGenerator.Emit(OpCodes.Ret);
        }

        private void DefineServiceReferenceTypeInvokeMethods(TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdProperty)
        {
            var voidType = typeof(void);
            var parentType = typeof(ServiceReference);
            var bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
            var argumentTypes = new Type[] { typeof(int), typeof(object[]), typeof(InvokeMethodOptions) };
            var invokeMethodMethod = parentType.GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.IsGenericMethod);
            var invokeOneWayMethodMethod = parentType.GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.ReturnType == typeof(void));
            var arrayEmptyMethod = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(object));

            ParameterInfo parameter;
            bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            foreach (var methodInfo in interfaceType.GetMethods(bindingAttr))
            {
                var methodId = ServiceUtility.ComputeMethodId(methodInfo);
                var operationContract = methodInfo.GetCustomAttribute<OperationContractAttribute>();
                var invokeMethodOptions = operationContract?.Options ?? InvokeMethodOptions.None;
                var parameters = methodInfo.GetParameters();
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                var methodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, CallingConventions.Standard, methodInfo.ReturnType, parameterTypes);
                for (int index = 0; index < parameters.Length; index++)
                {
                    parameter = parameters[index];
                    var parameterBuilder = methodBuilder.DefineParameter(index + 1, parameter.Attributes, parameter.Name);
                    foreach (var attribute in parameter.CustomAttributes)
                    {
                        var arguments = attribute.ConstructorArguments.Select(p => p.Value).ToArray();
                        var attributeBuilder = new CustomAttributeBuilder(attribute.Constructor, arguments);
                        parameterBuilder.SetCustomAttribute(attributeBuilder);
                    }
                }

                var iLGenerator = methodBuilder.GetILGenerator();
                if (methodInfo.ReturnType != voidType)
                {
                    iLGenerator.DeclareLocal(methodInfo.ReturnType);
                }

                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ldc_I4, methodId);
                if (parameters.Length > 0)
                {
                    EmitLdc(iLGenerator, parameters.Length);
                    iLGenerator.Emit(OpCodes.Newarr, typeof(object));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameter = parameters[i];
                        iLGenerator.Emit(OpCodes.Dup);
                        EmitLdc(iLGenerator, i);
                        EmitLdarg(iLGenerator, i + 1);
                        if (parameter.ParameterType.IsValueType)
                        {
                            iLGenerator.Emit(OpCodes.Box, parameter.ParameterType);
                        }
                        iLGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                }
                else
                {
                    iLGenerator.Emit(OpCodes.Ldnull);
                }

                //iLGenerator.Emit(OpCodes.Ldc_I4_0);
                EmitLdc(iLGenerator, (byte)invokeMethodOptions);
                if (methodInfo.ReturnType != voidType)
                {
                    var genericInvokeMethod = invokeMethodMethod.MakeGenericMethod(methodInfo.ReturnType);
                    iLGenerator.Emit(OpCodes.Call, genericInvokeMethod);
                    iLGenerator.Emit(OpCodes.Ret);
                }
                else
                {
                    iLGenerator.Emit(OpCodes.Call, invokeOneWayMethodMethod);
                    iLGenerator.Emit(OpCodes.Ret);
                }
            }
        }

        private Type CreateServiceReferenceType(Type interfaceType)
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}ServiceGeneric{1}ServiceReference", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;
            var typeBuilder = DefineType(typeFullName, typeAttributes, typeof(ServiceReference), new Type[] { interfaceType });
            DefineServiceReferenceTypeConstructors(typeBuilder);
            DefineServiceReferenceTypeInterfaceIdProperty(typeBuilder, interfaceType, out PropertyBuilder interfaceIdProperty);
            DefineServiceReferenceTypeGetMethodNameMethod(typeBuilder, interfaceType, interfaceIdProperty);
            DefineServiceReferenceTypeInvokeMethods(typeBuilder, interfaceType, interfaceIdProperty);
            return typeBuilder.CreateTypeInfo();
        }

        private ServiceTypeData CreateServiceTypeData(Type interfaceType)
        {
            var serviceReferenceType = CreateServiceReferenceType(interfaceType);
            var referenceCreator = new ServiceReferenceCreator((runtime) => Activator.CreateInstance(serviceReferenceType, new object[] { runtime }));
            return new ServiceTypeData(interfaceType, serviceReferenceType, referenceCreator);
        }

        public ServiceTypeData GetServiceTypeData(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("not interface type.");
            }
            var serviceContract = interfaceType.GetCustomAttribute<ServiceContractAttribute>();
            if (serviceContract == null)
            {
                throw new ArgumentException("not contract interface type.");
            }
            if (!serviceTypes.TryGetValue(interfaceType, out ServiceTypeData serviceTypeData))
            {
                serviceTypeData = CreateServiceTypeData(interfaceType);
                serviceTypes[interfaceType] = serviceTypeData;
            }

            return serviceTypeData;
        }
    }
}
