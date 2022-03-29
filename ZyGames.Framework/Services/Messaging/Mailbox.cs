using System.Threading;

namespace ZyGames.Framework.Services.Messaging
{
    public class Mailbox
    {
        private volatile int count;

        public int Count => count;

        public void Increment()
        {
            Interlocked.Increment(ref count);
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref count);
        }
    }
}
