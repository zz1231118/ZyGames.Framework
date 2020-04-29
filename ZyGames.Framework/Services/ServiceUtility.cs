using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ZyGames.Framework.Services
{
    internal static class ServiceUtility
    {
        private static int CalculateIdHash(string text)
        {
            byte[] result;
            byte[] data = Encoding.Unicode.GetBytes(text);
            using (var sha = SHA256.Create())
            {
                result = sha.ComputeHash(data);
            }

            var hash = 0;
            for (int i = 0; i < result.Length; i += 4)
            {
                hash ^= (result[i] << 24) | (result[i + 1] << 16) | (result[i + 2] << 8) | result[i + 3];
            }
            return hash;
        }

        private static Guid CalculateGuidHash(string text)
        {
            byte[] result;
            byte[] data = Encoding.Unicode.GetBytes(text);
            using (var sha = SHA256.Create())
            {
                result = sha.ComputeHash(data);
            }

            int index;
            byte[] hash = new byte[16];
            for (int i = 0; i < result.Length; i++)
            {
                index = i % 16;
                hash[index] = (byte)(hash[index] ^ result[i]);
            }
            return new Guid(hash);
        }

        private static int GetTypeCode(Type type)
        {
            return CalculateIdHash(type.AssemblyQualifiedName);
        }

        private static string FormatMethodForIdComputation(MethodInfo methodInfo)
        {
            var sb = new StringBuilder(methodInfo.Name);
            if (methodInfo.IsGenericMethodDefinition)
            {
                sb.Append('<');
                var genericArguments = methodInfo.GetGenericArguments();
                if (genericArguments.Length > 0)
                {
                    sb.Append(genericArguments[0].AssemblyQualifiedName);
                    for (int i = 1; i < genericArguments.Length; i++)
                    {
                        sb.Append(',');
                        sb.Append(genericArguments[i].AssemblyQualifiedName);
                    }
                }
                sb.Append('>');
            }
            sb.Append('(');

            Type parameterType;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                parameterType = parameters[0].ParameterType;
                if (parameterType.IsGenericParameter) sb.Append(parameterType.Name);
                else sb.Append(parameterType.AssemblyQualifiedName);
                for (int i = 1; i < parameters.Length; i++)
                {
                    sb.Append(',');
                    parameterType = parameters[i].ParameterType;
                    if (parameterType.IsGenericParameter) sb.Append(parameterType.Name);
                    else sb.Append(parameterType.AssemblyQualifiedName);
                }
            }
            sb.Append(')');
            return sb.ToString();
        }

        public static int GetInterfaceId(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            return GetTypeCode(interfaceType);
        }

        public static int ComputeMethodId(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var result = FormatMethodForIdComputation(methodInfo);
            return CalculateIdHash(result);
        }
    }
}