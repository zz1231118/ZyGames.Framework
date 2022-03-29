using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ZyGames.Framework.Services.Runtime.Generation
{
    internal abstract class TypeGenerator
    {
        protected readonly ModuleBuilder moduleBuilder;
        protected readonly Type interfaceType;
        protected readonly IList<MethodInfo> methods;

        public TypeGenerator(ModuleBuilder moduleBuilder, Type interfaceType, IList<MethodInfo> methods)
        {
            this.moduleBuilder = moduleBuilder;
            this.interfaceType = interfaceType;
            this.methods = methods;
        }

        protected static void EmitLdc(ILGenerator iLGenerator, int index)
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

        protected static void EmitLdarg(ILGenerator iLGenerator, int index)
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

        public abstract Type GenerateType();
    }
}
