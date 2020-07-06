using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Framework.Log;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services
{
    public sealed class ClusterMembershipService : SystemTarget, IClusterMembershipService
    {
        private readonly ILogger logger = Logger.GetLogger<ClusterMembershipService>();
        private readonly ConcurrentDictionary<SlioAddress, MembershipMember> membershipMembers = new ConcurrentDictionary<SlioAddress, MembershipMember>();
        private readonly MembershipVersion membershipVersion = new MembershipVersion();
        private ClusterMembershipServiceOptions membershipServiceOptions;
        private ClusterConnectionListener connectionListener;

        internal ClusterMembershipService()
        { }

        internal override Priority Priority => Priority.Core;

        private void ConnectionListener_Terminated(object sender, ClusterConnectionEventArgs e)
        {
            var connection = e.Connection;
            if (membershipMembers.TryRemove(connection.RemoteSlioAddress, out _))
            {
                var version = membershipVersion.Increment();
                NotifyMembershipTableChanged(version, null);
            }
        }

        private void NotifyMembershipTableChanged(MembershipVersion version, Identity applicant)
        {
            foreach (var member in membershipMembers.Values)
            {
                if (member.Identity != applicant)
                {
                    try
                    {
                        member.MembershipTableChanged(version);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("{0}.{1} error:{2}", nameof(ClusterMembershipService), nameof(TableChanged), ex);
                    }
                }
            }
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            membershipServiceOptions = ServiceProvider.GetRequiredService<ClusterMembershipServiceOptions>();

            Address = membershipServiceOptions.OutsideAddress;
            Identity = Constants.ClusterMembershipServiceIdentity;
        }

        protected internal override void Start()
        {
            base.Start();
            connectionListener = new ClusterConnectionListener(ServiceProvider, membershipServiceOptions);
            connectionListener.Terminated += new EventHandler<ClusterConnectionEventArgs>(ConnectionListener_Terminated);
            connectionListener.Start();
        }

        protected internal override void Stop()
        {
            base.Stop();
            connectionListener.Stop();
        }

        public bool Alive(MembershipEntry entry)
        {
            return membershipMembers.ContainsKey(entry.Address);
        }

        public MembershipSnapshot Register(MembershipTable table)
        {
            var entry = table.Entry;
            var systemTarget = ServiceFactory.GetSystemTarget<IGatewayMembershipService>(entry.Identity, entry.Address);
            var membershipMember = new MembershipMember(systemTarget, table);
            membershipMembers[membershipMember.Address] = membershipMember;
            NotifyMembershipTableChanged(membershipVersion.Increment(), membershipMember.Identity);
            return CreateSnapshot();
        }

        public void Unregister(MembershipEntry entry)
        {
            if (membershipMembers.TryRemove(entry.Address, out _))
            {
                NotifyMembershipTableChanged(membershipVersion.Increment(), null);
            }
        }

        public MembershipSnapshot CreateSnapshot()
        {
            var snapshot = new MembershipSnapshot();
            snapshot.Version = new MembershipVersion(membershipVersion);
            snapshot.Tables = new List<MembershipTable>();
            foreach (var member in membershipMembers.Values)
            {
                snapshot.Tables.Add(member.Table);
            }
            return snapshot;
        }

        public void TableChanged(MembershipTable table)
        {
            var entry = table.Entry;
            if (!membershipMembers.TryGetValue(entry.Address, out MembershipMember member))
            {
                logger.Warn("{0} MembershipMember:{1} not found.", nameof(TableChanged), table.Entry.Address);
                return;
            }

            member.Update(table);
            var version = membershipVersion.Increment();
            NotifyMembershipTableChanged(version, null);
        }

        public void KillService(Identity identity)
        {
            foreach (var member in membershipMembers.Values)
            {
                try
                {
                    member.KillService(identity);
                }
                catch (Exception ex)
                {
                    logger.Warn("{0}.{1} error:{2}", nameof(ClusterMembershipService), nameof(KillService), ex);
                }
            }
        }

        class MembershipMember
        {
            private readonly IGatewayMembershipService systemTarget;
            private MembershipTable table;

            public MembershipMember(IGatewayMembershipService systemTarget, MembershipTable table)
            {
                this.systemTarget = systemTarget;
                this.table = table;
            }

            public Identity Identity => table.Entry.Identity;

            public SlioAddress Address => table.Entry.Address;

            public MembershipTable Table => table;

            public void Update(MembershipTable table)
            {
                this.table = table;
            }

            public bool Contains(Identity identity)
            {
                return table.Rows.Exists(p => p.Identity == identity);
            }

            public void MembershipTableChanged(MembershipVersion version)
            {
                systemTarget.MembershipTableChanged(version);
            }

            public void KillService(Identity identity)
            {
                systemTarget.KillService(identity);
            }
        }
    }
}