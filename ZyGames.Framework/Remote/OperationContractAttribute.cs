using System;

namespace ZyGames.Framework.Remote
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OperationContractAttribute : Attribute
    {
        public InvokeMethodOptions Options { get; set; }
    }
}
