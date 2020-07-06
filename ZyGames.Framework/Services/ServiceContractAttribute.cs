using System;

namespace ZyGames.Framework.Services
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ServiceContractAttribute : Attribute
    {
        public ServiceContractAttribute()
        { }

        public ServiceContractAttribute(string guidString)
        {
            Guid = new Guid(guidString);
        }

        public ServiceContractAttribute(InvokeContextCategory invokeContextCategory)
        {
            InvokeContextCategory = invokeContextCategory;
        }

        public ServiceContractAttribute(string guidString, InvokeContextCategory invokeContextCategory)
        {
            Guid = new Guid(guidString);
            InvokeContextCategory = invokeContextCategory;
        }

        public Guid? Guid { get; set; }

        public InvokeContextCategory InvokeContextCategory { get; set; }
    }

    public enum InvokeContextCategory : byte
    {
        Multi,
        Single,
    }
}
