using System;
using Framework.Configuration;

namespace ZyGames.Framework.Services.Options
{
    public class GatewayMembershipServiceOptions : ConnectionListenerOptions
    {
        public SlioAddress Cluster { get; set; }

        public TimeSpan MembershipCheckingUpdatePeriod { get; set; } = TimeSpan.FromSeconds(5);

        public override void CopyFrom(Config conf)
        {
            base.CopyFrom(conf);
            Cluster = new SlioAddress(conf.GetString(nameof(Cluster)));
            MembershipCheckingUpdatePeriod = conf.GetTimeSpan(nameof(MembershipCheckingUpdatePeriod), TimeSpan.FromSeconds(5));
        }
    }
}
