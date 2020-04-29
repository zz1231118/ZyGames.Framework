using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZyGames.Framework.Services.Runtime
{
    internal class BinarySerializer
    {
        public byte[] Serialize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public object Deserialize(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream(bytes))
            {
                return formatter.Deserialize(ms);
            }
        }
    }
}
