using System;

namespace ZyGames.Framework.Injection
{
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string message)
            : base(message)
        { }
    }
}
