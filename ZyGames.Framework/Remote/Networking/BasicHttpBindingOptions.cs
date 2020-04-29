using System;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicHttpBindingOptions
    {
        public string Url { get; set; }

        public TimeSpan SessionCheckingInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan SessionCheckingTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
