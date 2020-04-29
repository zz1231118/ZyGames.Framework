using System;
using System.Collections.Generic;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public class MembershipSnapshot
    {
        public MembershipVersion Version { get; set; }

        public List<MembershipTable> Tables { get; set; }
    }
}
