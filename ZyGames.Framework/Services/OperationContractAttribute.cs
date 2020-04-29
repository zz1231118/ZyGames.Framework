using System;

namespace ZyGames.Framework.Services
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OperationContractAttribute : Attribute
    {
        public OperationContractAttribute()
        { }

        public OperationContractAttribute(InvokeMethodOptions options)
        {
            Options = options;
        }

        public InvokeMethodOptions Options { get; set; }
    }
}