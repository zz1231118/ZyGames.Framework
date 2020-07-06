using System;
using System.Runtime.Serialization;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public class ServiceRejectionException : Exception
    {
        public ServiceRejectionException(Message.RejectionTypes rejectionType)
        {
            RejectionType = rejectionType;
        }

        public ServiceRejectionException(Message.RejectionTypes rejectionType, Exception innerException)
            : base(rejectionType.ToString(), innerException)
        {
            RejectionType = rejectionType;
        }

        protected ServiceRejectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public Message.RejectionTypes RejectionType { get; }
    }
}
