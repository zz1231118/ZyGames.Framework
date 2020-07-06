using System;
using System.Runtime.Serialization;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public class NotSupportedMethodException : Exception
    {
        public NotSupportedMethodException(string message)
            : base(message)
        { }

        protected NotSupportedMethodException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
