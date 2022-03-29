using System;

namespace ZyGames.Framework.Services.Membership
{
    internal class ServiceLocator
    {
        public Address Address { get; set; }

        public Identity Identity { get; set; }

        public Type InterfaceType { get; set; }

        public object Metadata { get; set; }
    }
}
