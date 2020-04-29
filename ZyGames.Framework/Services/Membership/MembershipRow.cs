using System;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public class MembershipRow
    {
        public Identity Identity { get; set; }

        public string ServiceType { get; set; }

        public byte[] Metadata { get; set; }
    }
}
