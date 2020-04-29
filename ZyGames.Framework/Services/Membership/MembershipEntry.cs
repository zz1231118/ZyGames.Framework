using System;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public class MembershipEntry
    {
        public SlioAddress Address { get; set; }

        public Identity Identity { get; set; }
    }
}
