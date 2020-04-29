using System;
using ZyGames.Framework.Remote.Networking;

namespace ZyGames.Framework.Remote
{
    public abstract class Binding
    {
        public abstract ConnectionListener CreateConnectionListener(IServiceProvider serviceProvider);

        public abstract ClientRuntime CreateClientRuntime(IServiceProvider serviceProvider);
    }
}
