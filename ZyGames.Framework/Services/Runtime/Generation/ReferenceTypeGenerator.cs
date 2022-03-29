using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ZyGames.Framework.Services.Runtime.Generation
{
    internal sealed class ReferenceTypeGenerator : TypeGenerator
    {
        private readonly Type baseType;
        private TypeBuilder typeBuilder;
        private PropertyBuilder interfaceIdProperty;

        public ReferenceTypeGenerator(ModuleBuilder moduleBuilder, Type interfaceType, IList<MethodInfo> methods, Type baseType)
            : base(moduleBuilder, interfaceType, methods)
        {
            this.baseType = baseType;
        }

        private void DefineConstructors()
        {
            var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var baseTypeConstructor in baseType.GetConstructors(bindingAttr))
            {
                var parameters = baseTypeConstructor.GetParameters();
                var attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
                var callingConventions = CallingConventions.Standard | CallingConventions.HasThis;
                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
                var constructorBuilder = typeBuilder.DefineConstructor(attributes, callingConventions, parameterTypes);
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

                iLGenerator.Emit(OpCodes.Call, baseTypeConstructor);
                iLGenerator.Emit(OpCodes.Ret);
            }
        }

        private void DefineInterfaceIdProperty()
        {
            var interfaceId = ServiceInterfaceUtils.GetInterfaceId(interfaceType);
            var getterMethodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
            var getterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(Reference.InterfaceId)), getterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = getterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            interfaceIdProperty = typeBuilder.DefineProperty(nameof(Reference.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            interfaceIdProperty.SetGetMethod(getterMethodBuilder);
        }

        private void DefineGetMethodNameMethod()
        {
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notSupportedMethodExceptionConstructor = typeof(NotSupportedMethodException).GetConstructor(new Type[] { typeof(string) });

            var methodTemplate = baseType.GetMethod(nameof(Reference.GetMethodName));
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var parameters = methodTemplate.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(methodTemplate.Name, methodAttributes, CallingConventions.Standard, methodTemplate.ReturnType, parameterTypes);
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            }

            var iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.DeclareLocal(typeof(object));
            var retLabel = iLGenerator.DefineLabel();
            var defLabel = iLGenerator.DefineLabel();
            var jumpTable = new Label[methods.Count + 1];
            var bodyTable = new Label[methods.Count];
            jumpTable[methods.Count] = defLabel;
            for (int i = 0; i < bodyTable.Length; i++)
            {
                jumpTable[i] = iLGenerator.DefineLabel();
                bodyTable[i] = iLGenerator.DefineLabel();
            }
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                var methodId = ServiceInterfaceUtils.ComputeMethodId(method);
                iLGenerator.MarkLabel(jumpTable[i]);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Ldc_I4, methodId);
                iLGenerator.Emit(OpCodes.Beq, bodyTable[i]);
                iLGenerator.Emit(OpCodes.Br, jumpTable[i + 1]);
            }
            for (int i = 0; i < methods.Count; i++)
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

        private void DefineInvokeMethods()
        {
            var voidType = typeof(void);
            var bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
            var argumentTypes = new Type[] { typeof(int), typeof(object[]), typeof(InvokeMethodOptions) };
            var invokeMethodMethod = typeof(Reference).GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.IsGenericMethod);
            var invokeOneWayMethodMethod = typeof(Reference).GetMethods(bindingAttr).First(p => p.Name == "InvokeMethod" && p.ReturnType == typeof(void));
            var arrayEmptyMethod = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(object));

            ParameterInfo parameter;
            foreach (var method in methods)
            {
                var invokeMethodOptions = InvokeMethodOptions.None;
                var requestTimeoutMills = Constants.RequestTimeout.None;
                var methodId = ServiceInterfaceUtils.ComputeMethodId(method);
                var operationContract = method.GetCustomAttribute<OperationContractAttribute>();
                if (operationContract != null)
                {
                    if (operationContract.RequestTimeout < Constants.RequestTimeout.None)
                    {
                        throw new InvalidOperationException(string.Format("Addressable:{0}.{1} invalid request timeout:{2}", interfaceType.FullName, method.Name, operationContract.RequestTimeout));
                    }
                    if (operationContract.Options.HasFlag(InvokeMethodOptions.OneWay) && method.ReturnType != typeof(void))
                    {
                        throw new InvalidOperationException(string.Format("Addressable:{0}.{1} invalid return type:{2}", interfaceType.FullName, method.Name, method.ReturnType.FullName));
                    }

                    requestTimeoutMills = operationContract.RequestTimeout;
                    invokeMethodOptions = operationContract.Options;
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
                iLGenerator.Emit(OpCodes.Ldc_I4, requestTimeoutMills);
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

        public override Type GenerateType()
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}Generic{1}Reference", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;

            typeBuilder = moduleBuilder.DefineType(typeFullName, typeAttributes, baseType, new Type[] { interfaceType });
            DefineConstructors();
            DefineInterfaceIdProperty();
            DefineGetMethodNameMethod();
            DefineInvokeMethods();
            return typeBuilder.CreateTypeInfo();
        }
    }
}
