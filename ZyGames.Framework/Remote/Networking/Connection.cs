using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote.Networking
{
    public abstract class Connection
    {
        public abstract void SendMessage(Message message);
    }
}
