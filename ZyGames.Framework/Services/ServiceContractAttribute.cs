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

        public Guid? Guid { get; set; }
    }
}
