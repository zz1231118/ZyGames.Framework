using System;
using ZyGames.Framework.Security;

namespace ZyGames.Framework.Remote.Options
{
    public class ClientOptions
    {
        public static readonly ClientOptions Default = new ClientOptions();

        public IClientCredentials Credentials { get; set; }

        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);
    }
}