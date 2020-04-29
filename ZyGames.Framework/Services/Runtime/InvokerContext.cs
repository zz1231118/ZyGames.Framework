using System;

namespace ZyGames.Framework.Services.Runtime
{
    internal class InvokerContext
    {
        [ThreadStatic]
        private static IAddressable caller;

        public static IAddressable Caller
        {
            get => caller;
            set => caller = value;
        }
    }
}
