using System;

namespace ZyGames.Framework.Services
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    internal class SystemTargetContractAttribute : Attribute
    {
        public SystemTargetContractAttribute(Priority priority)
        {
            Priority = priority;
        }

        public Priority Priority { get; }
    }
}
