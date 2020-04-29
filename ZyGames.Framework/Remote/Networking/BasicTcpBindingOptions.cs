using System.Net;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicTcpBindingOptions
    {
        public EndPoint EndPoint { get; set; }

        public int Backlog { get; set; }

        public int MaxConnections { get; set; }
    }
}
