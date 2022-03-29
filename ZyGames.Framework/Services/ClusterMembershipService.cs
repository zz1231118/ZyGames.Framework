using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Framework.Injection;
using Framework.Log;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;

namespace ZyGames.Framework.Services
{
    public sealed class ClusterMembershipService : SystemTarget, IClusterMembershipService, IOptions<ClusterMembershipServiceOptions>
    {
        private readonly ILogger logger = Logger.GetLogger<ClusterMembershipService>();
        private readonly ConcurrentDictionary<Address, MembershipMember> membershipMembers = new ConcurrentDictionary<Address, MembershipMember>();
        private readonly MembershipVersion membershipVersion = new MembershipVersion();
        private ClusterMembershipServiceOptions membershipServiceOptions;
        private ClusterConnectionListener connectionListener;

        internal ClusterMembershipService()
        { }

        private void ConnectionListener_Terminated(object sender, ClusterConnectionEventArgs e)
        {
            var connection = e.Connection;
            if (membershipMembers.TryRemove(connection.RemoteSiloAddress, out _))
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
                        logger.Warn("{0}.{1} error:{2}", nameof(MembershipMember), nameof(MembershipMember.MembershipTableChanged), ex);
                    }
                }
            }
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            membershipServiceOptions = Container.Required<ClusterMembershipServiceOptions>();

            Address = membershipServiceOptions.OutsideAddress;
            Identity = Constants.ClusterMembershipServiceIdentity;
        }

        protected internal override void Start()
        {
            base.Start();
            connectionListener = new ClusterConnectionListener(Container, membershipServiceOptions);
            connectionListener.Terminated += new EventHandler<ClusterConnectionEventArgs>(ConnectionListener_Terminated);
            connectionListener.Start();
        }

        protected internal override void Stop()
        {
            base.Stop();
            connectionListener.Stop();
        }

        public bool Alive(MembershipEntry membershipEntry)
        {
            return membershipMembers.ContainsKey(membershipEntry.Address);
        }

        public MembershipSnapshot Register(MembershipTable membershipTable)
        {
            var membershipEntry = membershipTable.Entry;
            var systemTarget = ServiceFactory.GetSystemTarget<IGatewayMembershipService>(membershipEntry.Identity, membershipEntry.Address);
            var membershipMember = new MembershipMember(systemTarget, membershipTable);
            membershipMembers[membershipMember.Address] = membershipMember;
            NotifyMembershipTableChanged(membershipVersion.Increment(), membershipMember.Identity);
            return CreateMembershipSnapshot();
        }

        public void Unregister(MembershipEntry membershipEntry)
        {
            if (membershipMembers.TryRemove(membershipEntry.Address, out _))
            {
                NotifyMembershipTableChanged(membershipVersion.Increment(), null);
            }
        }

        public void MembershipTableChanged(MembershipTable membershipTable)
        {
            var membershipEntry = membershipTable.Entry;
            if (!membershipMembers.TryGetValue(membershipEntry.Address, out MembershipMember member))
            {
                logger.Warn("{0}.{1} MembershipMember:{2} not found.", nameof(ClusterMembershipService), nameof(MembershipTableChanged), membershipTable.Entry.Address);
                return;
            }

            member.Update(membershipTable);
            var version = membershipVersion.Increment();
            NotifyMembershipTableChanged(version, null);
        }

        public MembershipSnapshot CreateMembershipSnapshot()
        {
            var membershipSnapshot = new MembershipSnapshot();
            membershipSnapshot.Version = new MembershipVersion(membershipVersion);
            membershipSnapshot.Tables = new List<MembershipTable>();
            foreach (var member in membershipMembers.Values)
            {
                membershipSnapshot.Tables.Add(member.Table);
            }
            return membershipSnapshot;
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

        sealed class MembershipMember
        {
            private readonly IGatewayMembershipService systemTarget;
            private MembershipTable table;

            public MembershipMember(IGatewayMembershipService systemTarget, MembershipTable table)
            {
                this.systemTarget = systemTarget;
                this.table = table;
            }

            public Identity Identity => table.Entry.Identity;

            public Address Address => table.Entry.Address;

            public MembershipTable Table => table;

            public void Update(MembershipTable table)
            {
                this.table = table;
            }

            public bool Contains(Identity identity)
            {
                return table.Contains(identity);
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