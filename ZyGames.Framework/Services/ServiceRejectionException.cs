using System;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services
{
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

        public Message.RejectionTypes RejectionType { get; }
    }
}
