using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZyGames.Framework.Remote.Messaging
{
    public class MessageSerializer
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

        public Message Deserialize(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0 || offset >= bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream(bytes, offset, count))
            {
                return (Message)formatter.Deserialize(ms);
            }
        }

        public Message Deserialize(byte[] bytes)
        {
            return Deserialize(bytes, 0, bytes.Length);
        }
    }
}
