using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Framework.Log;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Membership
{
    internal class MembershipManager
    {
        private readonly ILogger logger = Logger.GetLogger<MembershipManager>();
        private readonly IServiceProvider serviceProvider;
        private readonly IMembershipLifecycle lifecycle;
        private readonly BinarySerializer binarySerializer;
        private MembershipVersion membershipVersion;
        private List<MembershipMember> membershipMembers;

        public MembershipManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.lifecycle = serviceProvider.GetRequiredService<IMembershipLifecycle>();
            this.binarySerializer = serviceProvider.GetRequiredService<BinarySerializer>();
        }

        public void UpdateFromSnapshot(MembershipSnapshot snapshot)
        {
            var members = new List<MembershipMember>();
            foreach (var table in snapshot.Tables)
            {
                var locators = new List<ServiceLocator>();
                foreach (var row in table.Rows)
                {
                    var interfaceType = Type.GetType(row.ServiceType, false);
                    if (interfaceType == null)
                    {
                        logger.Error("Address:{0} Identity:{1} Service:{2} type not found.", table.Entry.Address, row.Identity, row.ServiceType);
                        continue;
                    }

                    var locator = new ServiceLocator();
                    locator.Address = table.Entry.Address;
                    locator.Identity = row.Identity;
                    locator.InterfaceType = interfaceType;
                    if (row.Metadata != null)
                    {
                        locator.Metadata = binarySerializer.Deserialize(row.Metadata);
                    }

                    locators.Add(locator);
                }

                members.Add(new MembershipMember(table, locators));
            }

            membershipMembers = members;
            membershipVersion = snapshot.Version;
            lifecycle.NotifyObserver(Lifecycles.State.Membership.Changed);
        }

        public MembershipVersion Version => membershipVersion;

        public bool Alive(Identity identity)
        {
            return membershipMembers?.Exists(p => p.Contains(identity)) == true;
        }

        public ServiceLocator GetServiceLocator(Identity identity)
        {
            var members = membershipMembers;
            if (members != null)
            {
                ServiceLocator locator;
                foreach (var member in members)
                {
                    if (member.TryGet(identity, out locator))
                    {
                        return locator;
                    }
                }
            }

            return null;
        }

        public IReadOnlyList<ServiceLocator> GetServiceLocators(Type serviceInterfaceType)
        {
            var locators = new List<ServiceLocator>();
            var members = membershipMembers;
            if (members != null)
            {
                foreach (var member in members)
                {
                    foreach (var locator in member.Locators)
                    {
                        if (locator.InterfaceType == serviceInterfaceType)
                        {
                            locators.Add(locator);
                        }
                    }
                }
            }
            return locators;
        }

        class MembershipMember
        {
            private readonly MembershipTable table;
            private readonly ConcurrentDictionary<Identity, ServiceLocator> locators = new ConcurrentDictionary<Identity, ServiceLocator>();

            public MembershipMember(MembershipTable table, IEnumerable<ServiceLocator> locators)
            {
                this.table = table;
                foreach (var locator in locators)
                {
                    this.locators[locator.Identity] = locator;
                }
            }

            public MembershipEntry Entry => table.Entry;

            public SlioAddress Address => table.Entry.Address;

            public ICollection<ServiceLocator> Locators => locators.Values;

            public bool TryGet(Identity identity, out ServiceLocator locator)
            {
                return locators.TryGetValue(identity, out locator);
            }

            public bool Contains(Identity identity)
            {
                return locators.ContainsKey(identity);
            }
        }
    }
}
