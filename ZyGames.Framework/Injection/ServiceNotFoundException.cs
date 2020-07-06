using System;
using System.Runtime.Serialization;

namespace ZyGames.Framework.Injection
{
    [Serializable]
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string message)
            : base(message)
        { }

        protected ServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
