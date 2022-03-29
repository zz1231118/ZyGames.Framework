using System;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public class MembershipEntry
    {
        public Address Address { get; set; }

        public Identity Identity { get; set; }
    }
}
