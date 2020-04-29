using System;
using System.Collections.Concurrent;
using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote.Networking
{
    public abstract class ClientRuntime : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, CompletionSource<object>> contexts = new ConcurrentDictionary<Guid, CompletionSource<object>>();
        private bool isDisposed;

        protected bool IsDisposed => isDisposed;

        protected void Dispatch(Message message)
        {
            switch (message.Direction)
            {
                case Message.Directions.Response:
                    if (contexts.TryGetValue(message.Guid, out CompletionSource<object> context))
                    {
                        switch (message.Result)
                        {
                            case Message.ResponseTypes.Success:
                                context.SetResult(message.BodyObject);
                                break;
                            case Message.ResponseTypes.Error:
                                context.SetException((Exception)message.BodyObject);
                                break;
                        }
                    }
                    break;
            }
        }

        protected void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        public abstract void SendMessage(Message message);

        public object SendRequest(Message message, TimeSpan timeout)
        {
            using (var context = new CompletionSource<object>())
            {
                contexts[message.Guid] = context;
                SendMessage(message);

                try
                {
                    return context.GetResult(timeout);
                }
                finally
                {
                    contexts.TryRemove(message.Guid, out _);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
