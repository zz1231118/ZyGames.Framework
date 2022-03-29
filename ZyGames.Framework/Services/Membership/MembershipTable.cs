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

        public bool Contains(Identity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            for (int i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].Identity == identity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
