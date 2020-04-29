using System.Collections.Generic;

namespace ZyGames.Framework.Services.Messaging
{
    internal class Mailbox
    {
        private readonly Queue<Message> messages = new Queue<Message>();

        public int Count => messages.Count;

        public void Enqueue(Message message)
        {
            messages.Enqueue(message);
        }

        public Message Dequeue()
        {
            return messages.Dequeue();
        }

        public bool TryDequeue(out Message message)
        {
            if (messages.Count > 0)
            {
                message = messages.Dequeue();
                return true;
            }

            message = null;
            return false;
        }
    }
}
