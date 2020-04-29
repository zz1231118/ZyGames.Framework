using System;
using System.Collections.Concurrent;
using ZyGames.Framework.Services.Messaging;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    internal class InvokableObjectManager
    {
        private readonly ConcurrentDictionary<Guid, IMethodInvoker> localObjects = new ConcurrentDictionary<Guid, IMethodInvoker>();

        private void Invoke(IMethodInvoker invoker, Message message)
        { }

        public void Dispatch(Message message)
        { }
    }
}
