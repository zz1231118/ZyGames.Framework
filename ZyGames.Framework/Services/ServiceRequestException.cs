using System;
using System.Runtime.Serialization;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public class ServiceRequestException : Exception
    {
        public ServiceRequestException()
        { }

        public ServiceRequestException(string message)
            : base(message)
        { }

        public ServiceRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected ServiceRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
