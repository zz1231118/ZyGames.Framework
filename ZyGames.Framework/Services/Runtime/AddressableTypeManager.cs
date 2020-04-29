using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ZyGames.Framework.Services.Messaging;

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

        private void EmitLdc(ILGenerator iLGenerator, int index)
        {
            switch (index)
            {
                case 0:
                    iLGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    iLGenerator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    iLGenerator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    iLGenerator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    iLGenerator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    iLGenerator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    iLGenerator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    iLGenerator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    iLGenerator.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    iLGenerator.Emit(OpCodes.Ldc_I4_S, index);
                    break;
            }
        }

        private void EmitLdarg(ILGenerator iLGenerator, int index)
        {
            switch (index)
            {
                case 0:
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    iLGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    iLGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    iLGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    iLGenerator.Emit(OpCodes.Ldarg_S, index);
                    break;
            }
        }


        private void DefineMethodInvokerTypeConstructors(TypeBuilder typeBuilder)
        {
            var parentType = typeof(object);
            var parentConstructor = parentType.GetConstructor(Type.EmptyTypes);
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var iLGenerator = constructorBuilder.GetILGenerator();
            iLGenerator.DeclareLocal(parentType);
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Call, parentConstructor);
            iLGenerator.Emit(OpCodes.Ret);
        }

        private void DefineMethodInvokerTypeInterfaceIdProperty(TypeBuilder typeBuilder, Type interfaceType, out PropertyBuilder interfaceIdProperty)
        {
            var interfaceId = ServiceUtility.GetInterfaceId(interfaceType);
            var interfaceIdGetterMethodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var interfaceIdGetterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(IMethodInvoker.InterfaceId)), interfaceIdGetterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = interfaceIdGetterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            var interfaceIdPropertyBuilder = typeBuilder.DefineProperty(nameof(IMethodInvoker.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            interfaceIdPropertyBuilder.SetGetMethod(interfaceIdGetterMethodBuilder);

            interfaceIdProperty = interfaceIdPropertyBuilder;
        }

        private void DefineMethodInvokerTypeGetMethodNameMethod(TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdProperty)
        {
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notSupportedMethodExceptionConstructor = typeof(NotSupportedMethodException).GetConstructor(new Type[] { typeof(string) });

            var methodTemplate = typeof(IMethodInvoker).GetMethod(nameof(IMethodInvoker.GetMethodName));
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
            iLGenerator.Emit(OpCodes.Newobj, notSupportedMethodExceptionConstructor);
            iLGenerator.Emit(OpCodes.Throw);

            iLGenerator.MarkLabel(retLabel);
            iLGenerator.Emit(OpCodes.Ldloc_0);
            iLGenerator.Emit(OpCodes.Ret);
        }

        private void DefineMethodInvokerTypeInvokeMethod(TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdPropertyBuilder)
        {
            var interfaceMethod = typeof(IMethodInvoker).GetMethod(nameof(IMethodInvoker.Invoke));
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notSupportedMethodExceptionConstructor = typeof(NotSupportedMethodException).GetConstructor(new Type[] { typeof(string) });
            var getMethodIdProperty = typeof(InvokeMethodRequest).GetProperty(nameof(InvokeMethodRequest.MethodId));
            var getArgumentsProperty = typeof(InvokeMethodRequest).GetProperty(nameof(InvokeMethodRequest.Arguments));
            var methodParameterTypes = new Type[] { typeof(IAddressable), typeof(InvokeMethodRequest) };
            var methodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var methodBuilder = typeBuilder.DefineMethod(nameof(IMethodInvoker.Invoke), methodAttributes, CallingConventions.HasThis, typeof(object), methodParameterTypes);
            var parameters = interfaceMethod.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            }

            var iLGenerator = methodBuilder.GetILGenerator();
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
            iLGenerator.Emit(OpCodes.Newobj, notSupportedMethodExceptionConstructor);
            iLGenerator.Emit(OpCodes.Throw);

            iLGenerator.MarkLabel(retLabel);
            iLGenerator.Emit(OpCodes.Ldloc_1);
            iLGenerator.Emit(OpCodes.Ret);
        }


        private void DefineReferenceTypeConstructors(Type baseType, TypeBuilder typeBuilder)
        {
            var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var parentConstructors = baseType.GetConstructors(bindingAttr);
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
                iLGenerator.DeclareLocal(baseType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                for (int i = 1; i <= parameters.Length; i++)
                {
                    EmitLdarg(iLGenerator, i);
                }

                iLGenerator.Emit(OpCodes.Call, parentConstructor);
                iLGenerator.Emit(OpCodes.Ret);
            }
        }

        private void DefineReferenceTypeInterfaceIdProperty(TypeBuilder typeBuilder, Type interfaceType, out PropertyBuilder propertyBuilder)
        {
            var interfaceId = ServiceUtility.GetInterfaceId(interfaceType);
            var getterMethodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
            var getterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(Reference.InterfaceId)), getterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = getterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            propertyBuilder = typeBuilder.DefineProperty(nameof(Reference.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            propertyBuilder.SetGetMethod(getterMethodBuilder);
        }

        private void DefineReferenceTypeGetMethodNameMethod(Type baseType, TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdProperty)
        {
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notSupportedMethodExceptionConstructor = typeof(NotSupportedMethodException).GetConstructor(new Type[] { typeof(string) });

            var methodTemplate = baseType.GetMethod(nameof(Reference.GetMethodName));
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
            iLGenerator.Emit(OpCodes.Newobj, notSupportedMethodExceptionConstructor);
            iLGenerator.Emit(OpCodes.Throw);

            iLGenerator.MarkLabel(retLabel);
            iLGenerator.Emit(OpCodes.Ldloc_0);
            iLGenerator.Emit(OpCodes.Ret);
        }

        private void DefineReferenceTypeInvokeMethods(Type baseType, TypeBuilder typeBuilder, Type interfaceType, PropertyBuilder interfaceIdProperty)
        {
            var voidType = typeof(void);
            var bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
            var argumentTypes = new Type[] { typeof(int), typeof(object[]), typeof(InvokeMethodOptions) };
            var invokeMethodMethod = typeof(Reference).GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.IsGenericMethod);
            var invokeOneWayMethodMethod = typeof(Reference).GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.ReturnType == typeof(void));
            var arrayEmptyMethod = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(object));

            ParameterInfo parameter;
            bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            foreach (var method in interfaceType.GetMethods(bindingAttr))
            {
                var methodId = ServiceUtility.ComputeMethodId(method);
                var operationContract = method.GetCustomAttribute<OperationContractAttribute>();
                var invokeMethodOptions = operationContract?.Options ?? InvokeMethodOptions.None;
                if (invokeMethodOptions.HasFlag(InvokeMethodOptions.OneWay) && method.ReturnType != typeof(void))
                {
                    throw new InvalidOperationException(string.Format("Addressable:{0}.{1} invalid return type:{2}", interfaceType.FullName, method.Name, method.ReturnType.FullName));
                }

                var parameters = method.GetParameters();
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                var methodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
                var methodBuilder = typeBuilder.DefineMethod(method.Name, methodAttributes, CallingConventions.Standard, method.ReturnType, parameterTypes);
                for (int index = 0; index < parameters.Length; index++)
                {
                    parameter = parameters[index];
                    methodBuilder.DefineParameter(index + 1, ParameterAttributes.None, parameter.Name);
                }

                var iLGenerator = methodBuilder.GetILGenerator();
                if (method.ReturnType != voidType)
                {
                    iLGenerator.DeclareLocal(method.ReturnType);
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
                if (method.ReturnType != voidType)
                {
                    var genericInvokeMethod = invokeMethodMethod.MakeGenericMethod(method.ReturnType);
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


        protected Type CreateReferenceType(Type baseType, Type interfaceType)
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}Generic{1}Reference", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;
            var typeBuilder = moduleBuilder.DefineType(typeFullName, typeAttributes, baseType, new Type[] { interfaceType });
            DefineReferenceTypeConstructors(baseType, typeBuilder);
            DefineReferenceTypeInterfaceIdProperty(typeBuilder, interfaceType, out PropertyBuilder interfaceIdProperty);
            DefineReferenceTypeGetMethodNameMethod(baseType, typeBuilder, interfaceType, interfaceIdProperty);
            DefineReferenceTypeInvokeMethods(baseType, typeBuilder, interfaceType, interfaceIdProperty);
            return typeBuilder.CreateTypeInfo();
        }

        protected Type CreateMethodInvokerType(Type interfaceType)
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}Generic{1}MethodInvoker", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;
            var typeBuilder = moduleBuilder.DefineType(typeFullName, typeAttributes, typeof(object), new Type[] { typeof(IMethodInvoker) });
            DefineMethodInvokerTypeConstructors(typeBuilder);
            DefineMethodInvokerTypeInterfaceIdProperty(typeBuilder, interfaceType, out PropertyBuilder interfaceIdProperty);
            DefineMethodInvokerTypeGetMethodNameMethod(typeBuilder, interfaceType, interfaceIdProperty);
            DefineMethodInvokerTypeInvokeMethod(typeBuilder, interfaceType, interfaceIdProperty);
            return typeBuilder.CreateTypeInfo();
        }

        private AddressableTypeData GetAddressableTypeData(Type baseType, Type interfaceType)
        {
            if (!addressTypes.TryGetValue(interfaceType, out AddressableTypeData addressTypeData))
            {
                var referenceType = CreateReferenceType(baseType, interfaceType);
                var invokerType = CreateMethodInvokerType(interfaceType);
                addressTypeData = new AddressableTypeData(interfaceType, referenceType, invokerType);
                addressTypes[interfaceType] = addressTypeData;
            }

            return addressTypeData;
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
