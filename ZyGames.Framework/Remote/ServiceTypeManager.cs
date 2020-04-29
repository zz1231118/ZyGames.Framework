using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ZyGames.Framework.Remote
{
    internal abstract class ServiceTypeManager
    {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;

        public ServiceTypeManager()
        {
            var assemblyName = new AssemblyName("DynamicServiceAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        protected void EmitLdc(ILGenerator iLGenerator, int value)
        {
            switch (value)
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
                    iLGenerator.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        protected void EmitLdarg(ILGenerator iLGenerator, int value)
        {
            switch (value)
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
                    iLGenerator.Emit(OpCodes.Ldarg, value);
                    break;
            }
        }

        protected TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            return moduleBuilder.DefineType(name, attr, parent, interfaces);
        }
    }
}