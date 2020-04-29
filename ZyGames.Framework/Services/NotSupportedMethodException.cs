using System;

namespace ZyGames.Framework.Services
{
    public class NotSupportedMethodException : Exception
    {
        public NotSupportedMethodException(string message)
            : base(message)
        { }
    }
}
