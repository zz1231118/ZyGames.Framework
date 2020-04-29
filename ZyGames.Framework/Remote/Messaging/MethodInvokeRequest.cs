using System;

namespace ZyGames.Framework.Remote.Messaging
{
    [Serializable]
    public class MethodInvokeRequest
    {
        public int MethodId { get; internal set; }

        public object[] Arguments { get; internal set; }
    }
}
