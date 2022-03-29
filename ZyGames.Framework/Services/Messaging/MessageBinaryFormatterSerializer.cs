using System;
using System.Runtime.Serialization.Formatters.Binary;
using Framework.IO;

namespace ZyGames.Framework.Services.Messaging
{
    internal class MessageBinaryFormatterSerializer : IMessageSerializer
    {
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        public byte[] Serialize(Message message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var formatter = new BinaryFormatter();
            using (var ms = recyclableMemoryStreamManager.GetStream(nameof(MessageBinaryFormatterSerializer)))
            {
                formatter.Serialize(ms, message);
                return ms.ToArray();
            }
        }

        public Message Deserialize(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var ms = recyclableMemoryStreamManager.GetStream(nameof(MessageBinaryFormatterSerializer), bytes))
            {
                return (Message)formatter.Deserialize(ms);
            }
        }
    }
}
