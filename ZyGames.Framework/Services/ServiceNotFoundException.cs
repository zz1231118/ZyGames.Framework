using System;
using System.Runtime.Serialization;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException()
        { }

        public ServiceNotFoundException(Identity identity)
            : base(string.Format("service:{0} not found.", identity))
        { }

        public ServiceNotFoundException(Identity identity, Exception innerException)
            : base(string.Format("service:{0} not found.", identity), innerException)
        { }

        protected ServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
