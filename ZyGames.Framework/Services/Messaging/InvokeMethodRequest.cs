using System;

namespace ZyGames.Framework.Services.Messaging
{
    [Serializable]
    public class InvokeMethodRequest
    {
        public int MethodId { get; set; }

        public object[] Arguments { get; set; }
    }
}
