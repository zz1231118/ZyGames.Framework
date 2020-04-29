using System;
using System.Collections.Generic;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public class MembershipTable
    {
        public MembershipEntry Entry { get; set; }

        public MembershipVersion Version { get; set; }

        public List<MembershipRow> Rows { get; set; }
    }
}
