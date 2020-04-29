using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZyGames.Framework.Services.Messaging
{
    internal class MessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(Message message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, message);
                return ms.ToArray();
            }
        }

        public Message Deserialize(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream(bytes))
            {
                return (Message)formatter.Deserialize(ms);
            }
        }
    }
}
