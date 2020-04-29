using System;

namespace ZyGames.Framework.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceContractAttribute : Attribute
    {
        public ServiceContractAttribute()
        { }

        public ServiceContractAttribute(InvokeContextCategory invokeContextCategory)
        {
            InvokeContextCategory = invokeContextCategory;
        }

        public InvokeContextCategory InvokeContextCategory { get; set; }
    }

    public enum InvokeContextCategory : byte
    {
        Multi,
        Single,
    }
}
