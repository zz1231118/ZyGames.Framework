using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Runtime.Generation
{
    internal sealed class InvokerTypeGenerator : TypeGenerator
    {
        private TypeBuilder typeBuilder;
        private PropertyBuilder interfaceIdProperty;

        public InvokerTypeGenerator(ModuleBuilder moduleBuilder, Type interfaceType, IList<MethodInfo> methods)
            : base(moduleBuilder, interfaceType, methods)
        { }

        private void DefineConstructors()
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

        private void DefineInterfaceIdProperty()
        {
            var interfaceId = ServiceInterfaceUtils.GetInterfaceId(interfaceType);
            var interfaceIdGetterMethodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            var interfaceIdGetterMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", nameof(IMethodInvoker.InterfaceId)), interfaceIdGetterMethodAttributes, typeof(int), Type.EmptyTypes);
            var iLGenerator = interfaceIdGetterMethodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldc_I4, interfaceId);
            iLGenerator.Emit(OpCodes.Ret);

            var interfaceIdPropertyBuilder = typeBuilder.DefineProperty(nameof(IMethodInvoker.InterfaceId), PropertyAttributes.None, CallingConventions.HasThis, typeof(int), Type.EmptyTypes);
            interfaceIdPropertyBuilder.SetGetMethod(interfaceIdGetterMethodBuilder);

            interfaceIdProperty = interfaceIdPropertyBuilder;
        }

        private void DefineGetMethodNameMethod()
        {
            var stringFormatMethod = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
            var notSupportedMethodExceptionConstructor = typeof(NotSupportedMethodException).GetConstructor(new Type[] { typeof(string) });

            var methodTemplate = typeof(IMethodInvoker).GetMethod(nameof(IMethodInvoker.GetMethodName));
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

        private void DefineInvokeMethod()
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

            var retLabel = iLGenerator.DefineLabel();
            var defLabel = iLGenerator.DefineLabel();
            var jumpTable = new Label[methods.Count];
            var bodyTable = new Label[methods.Count];
            for (int i = 0; i < bodyTable.Length; i++)
            {
                jumpTable[i] = iLGenerator.DefineLabel();
                bodyTable[i] = iLGenerator.DefineLabel();
            }
            var endSentinel = methods.Count - 1;
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                var methodId = ServiceInterfaceUtils.ComputeMethodId(method);
                iLGenerator.MarkLabel(jumpTable[i]);
                iLGenerator.Emit(OpCodes.Ldloc_0);
                iLGenerator.Emit(OpCodes.Ldc_I4, methodId);
                iLGenerator.Emit(OpCodes.Beq, bodyTable[i]);
                iLGenerator.Emit(OpCodes.Br, i < endSentinel ? jumpTable[i + 1] : defLabel);
            }
            for (int i = 0; i < methods.Count; i++)
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
            iLGenerator.Emit(OpCodes.Call, interfaceIdProperty.GetMethod);
            iLGenerator.Emit(OpCodes.Box, interfaceIdProperty.PropertyType);
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

        public override Type GenerateType()
        {
            var @namespace = interfaceType.Namespace != null ? interfaceType.Namespace + "." : string.Empty;
            var genericTypeName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
            var typeFullName = string.Format("{0}Generic{1}MethodInvoker", @namespace, genericTypeName);
            var typeAttributes = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class;
            typeBuilder = moduleBuilder.DefineType(typeFullName, typeAttributes, typeof(object), new Type[] { typeof(IMethodInvoker) });
            DefineConstructors();
            DefineInterfaceIdProperty();
            DefineGetMethodNameMethod();
            DefineInvokeMethod();
            return typeBuilder.CreateTypeInfo();
        }
    }
}
