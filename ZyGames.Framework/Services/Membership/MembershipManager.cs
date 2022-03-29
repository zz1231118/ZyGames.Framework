using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Framework.Injection;
using Framework.Log;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Membership
{
    internal class MembershipManager
    {
        private readonly ILogger logger = Logger.GetLogger<MembershipManager>();
        private readonly IMembershipLifecycle lifecycle;
        private readonly BinarySerializer binarySerializer;
        private MembershipVersion membershipVersion;
        private List<MembershipMember> membershipMembers;

        public MembershipManager(IContainer container)
        {
            this.lifecycle = container.Required<IMembershipLifecycle>();
            this.binarySerializer = container.Required<BinarySerializer>();
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
            lifecycle.Notify(Lifecycles.State.Membership.Changed);
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
                foreach (var member in members)
                {
                    if (member.TryGet(identity, out ServiceLocator locator))
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
                    foreach (var serviceLocator in member.ServiceLocators)
                    {
                        if (serviceInterfaceType.IsAssignableFrom(serviceLocator.InterfaceType))
                        {
                            locators.Add(serviceLocator);
                        }
                    }
                }
            }
            return locators;
        }

        class MembershipMember
        {
            private readonly MembershipTable membershipTable;
            private readonly ConcurrentDictionary<Identity, ServiceLocator> serviceLocators = new ConcurrentDictionary<Identity, ServiceLocator>();

            public MembershipMember(MembershipTable membershipTable, IEnumerable<ServiceLocator> serviceLocators)
            {
                this.membershipTable = membershipTable;
                foreach (var serviceLocator in serviceLocators)
                {
                    this.serviceLocators[serviceLocator.Identity] = serviceLocator;
                }
            }

            public MembershipEntry Entry => membershipTable.Entry;

            public Address Address => membershipTable.Entry.Address;

            public ICollection<ServiceLocator> ServiceLocators => serviceLocators.Values;

            public bool TryGet(Identity identity, out ServiceLocator locator)
            {
                return serviceLocators.TryGetValue(identity, out locator);
            }

            public bool Contains(Identity identity)
            {
                return serviceLocators.ContainsKey(identity);
            }
        }
    }
}
