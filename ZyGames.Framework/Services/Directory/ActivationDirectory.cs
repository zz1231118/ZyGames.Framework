using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Framework.Injection;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Directory
{
    internal sealed class ActivationDirectory : ILifecycleObserver
    {
        private readonly ConcurrentDictionary<Identity, Activation> activations = new ConcurrentDictionary<Identity, Activation>();
        private readonly MembershipVersion membershipVersion = new MembershipVersion();
        private readonly IServiceHostLifecycle hostingLifecycle;
        private readonly IDirectoryLifecycle directoryLifecycle;
        private readonly BinarySerializer binarySerializer;

        public ActivationDirectory(IContainer container)
        {
            this.hostingLifecycle = container.Required<IServiceHostLifecycle>();
            this.directoryLifecycle = container.Required<IDirectoryLifecycle>();
            this.binarySerializer = container.Required<BinarySerializer>();
            this.hostingLifecycle.Subscribe<ActivationDirectory>(Lifecycles.Stage.Core, this);
        }

        public ICollection<Activation> Activations => activations.Values;

        public MembershipVersion Version => membershipVersion;

        public void RegisterTarget(Activation activation)
        {
            if (!activations.TryAdd(activation.Identity, activation))
                throw new InvalidOperationException("repeatitive identity");

            membershipVersion.Increment();
            directoryLifecycle.Notify(Lifecycles.State.ActivationDirectory.Changed);
        }

        public Activation FindTarget(Identity identity)
        {
            activations.TryGetValue(identity, out Activation activation);
            return activation;
        }

        public bool KillTarget(Identity identity)
        {
            if (activations.TryRemove(identity, out Activation activation))
            {
                activation.Stop();
                membershipVersion.Increment();
                directoryLifecycle.Notify(Lifecycles.State.ActivationDirectory.Changed);
                return true;
            }

            return false;
        }

        public bool Contains(Identity identity)
        {
            return activations.ContainsKey(identity);
        }

        public MembershipTable CreateMembershipTable()
        {
            var table = new MembershipTable();
            table.Version = membershipVersion;
            table.Rows = new List<MembershipRow>();
            foreach (var activation in activations.Values)
            {
                if (activation.Identity.Category == Identity.Categories.Service)
                {
                    var service = (IService)activation.Addressable;
                    var row = new MembershipRow();
                    row.Identity = activation.Identity;
                    row.ServiceType = activation.InterfaceType.AssemblyQualifiedName;
                    if (service.Metadata != null)
                    {
                        row.Metadata = binarySerializer.Serialize(service.Metadata);
                    }

                    table.Rows.Add(row);
                }
            }
            return table;
        }

        void ILifecycleObserver.Notify(CancellationToken token, int state)
        {
            switch (state)
            {
                case Lifecycles.State.ServiceHost.Starting:
                    foreach (var activation in activations.Values.OrderBy(p => p.Priority))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        activation.Start();
                    }
                    break;
                case Lifecycles.State.ServiceHost.Stopped:
                    foreach (var activation in activations.Values.OrderByDescending(p => p.Priority))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        activation.Stop();
                    }
                    break;
            }
        }
    }
}
