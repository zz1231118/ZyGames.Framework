namespace ZyGames.Framework.Services.Messaging
{
    public interface IMessageSerializer
    {
        byte[] Serialize(Message message);

        Message Deserialize(byte[] bytes);
    }
}
