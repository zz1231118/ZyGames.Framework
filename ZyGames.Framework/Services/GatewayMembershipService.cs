using System;
using System.Threading;
using Framework.Log;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    public sealed class GatewayMembershipService : SystemTarget, IGatewayMembershipService, ILifecycleObserver
    {
        private readonly ILogger logger = Logger.GetLogger<GatewayMembershipService>();
        private GatewayMembershipServiceOptions membershipServiceOptions;
        private ActivationDirectory activationDirectory;
        private AddressableDirectory addressableDirectory;
        private MembershipManager membershipManager;
        private GatewayConnectionListener connectionListener;
        private IServiceHostLifecycle hostingLifecycle;
        private IClusterMembershipService clusterMembershipService;
        private Timer membershipUpdateTimer;
        private MembershipEntry membershipEntry;
        private bool isAlived;

        internal GatewayMembershipService()
        { }

        internal override Priority Priority => Priority.System;

        private MembershipTable CreateMembershipTable()
        {
            var membershipTable = activationDirectory.CreateMembershipTable();
            membershipTable.Entry = membershipEntry;
            return membershipTable;
        }

        private void MembershipChecking()
        {
            try
            {
                if (!clusterMembershipService.Alive(membershipEntry))
                {
                    var membershipTable = CreateMembershipTable();
                    var membershipSnapshot = clusterMembershipService.Register(membershipTable);
                    membershipManager.UpdateFromSnapshot(membershipSnapshot);
                    if (hostingLifecycle.Status <= ServiceHostStatus.Joining)
                    {
                        hostingLifecycle.NotifyObserver(Lifecycles.State.ServiceHost.Started);
                    }

                    isAlived = true;
                }
            }
            catch (Exception ex)
            {
                isAlived = false;
                //applicationLifecycle.NotifyObserver(ApplicationLifecycle.Starting);
                logger.Warn("{0}.{1} error:{2}", nameof(GatewayMembershipService), nameof(MembershipChecking), ex);
            }
        }

        private void MembershipCheckingCallback(object obj)
        {
            var updatePeriod = Timeout.InfiniteTimeSpan;
            membershipUpdateTimer.Change(updatePeriod, updatePeriod);

            InvokerContext.Caller = this;
            try
            {
                MembershipChecking();
            }
            finally
            {
                InvokerContext.Caller = null;
            }

            updatePeriod = membershipServiceOptions.MembershipUpdatePeriod;
            membershipUpdateTimer.Change(updatePeriod, updatePeriod);
        }

        internal protected override void Initialize()
        {
            base.Initialize();
            this.membershipServiceOptions = ServiceProvider.GetRequiredService<GatewayMembershipServiceOptions>();
            this.activationDirectory = ServiceProvider.GetRequiredService<ActivationDirectory>();
            this.addressableDirectory = ServiceProvider.GetRequiredService<AddressableDirectory>();
            this.membershipManager = ServiceProvider.GetRequiredService<MembershipManager>();
            this.hostingLifecycle = ServiceProvider.GetRequiredService<IServiceHostLifecycle>();

            Address = membershipServiceOptions.OutsideAddress;
            Identity = Identity.NewIdentity(Identity.Categories.SystemTarget);

            membershipEntry = new MembershipEntry();
            membershipEntry.Identity = Identity;
            membershipEntry.Address = Address;

            clusterMembershipService = ServiceFactory.GetSystemTarget<IClusterMembershipService>(Constants.ClusterMembershipServiceIdentity, membershipServiceOptions.Cluster);
            var lifecycle = ServiceProvider.GetRequiredService<IDirectoryLifecycle>();
            lifecycle.Subscribe(nameof(GatewayMembershipService), Lifecycles.Stage.System, this);
        }

        internal protected override void Start()
        {
            base.Start();
            connectionListener = new GatewayConnectionListener(ServiceProvider, membershipServiceOptions);
            connectionListener.Start();

            MembershipChecking();

            var updatePeriod = membershipServiceOptions.MembershipUpdatePeriod;
            membershipUpdateTimer = new Timer(new TimerCallback(MembershipCheckingCallback), null, updatePeriod, updatePeriod);
        }

        internal protected override void Stop()
        {
            base.Stop();
            if (isAlived)
            {
                try
                {
                    clusterMembershipService.Unregister(membershipEntry);
                }
                catch (Exception ex)
                {
                    logger.Warn("{0}.{1} error:{2}", nameof(GatewayMembershipService), nameof(Stop), ex);
                }
            }

            membershipUpdateTimer.Dispose();
            connectionListener.Stop();
        }

        public void MembershipTableChanged(MembershipVersion version)
        {
            if (membershipManager.Version == null || membershipManager.Version < version)
            {
                MembershipSnapshot membershipSnapshot;

                try
                {
                    membershipSnapshot = clusterMembershipService.CreateSnapshot();
                }
                catch (Exception ex)
                {
                    logger.Warn("{0}.{1} error:{2}", nameof(GatewayMembershipService), nameof(IClusterMembershipService.CreateSnapshot), ex);
                    return;
                }

                membershipManager.UpdateFromSnapshot(membershipSnapshot);
            }
        }

        public void KillService(Identity identity)
        {
            activationDirectory.KillTarget(identity);
            addressableDirectory.Remove(identity);
        }

        void ILifecycleObserver.Notify(CancellationToken token, int state)
        {
            switch (state)
            {
                case Lifecycles.State.ActivationDirectory.Changed:
                    if (isAlived)
                    {
                        var membershipTable = CreateMembershipTable();
                        clusterMembershipService.TableChanged(membershipTable);
                    }
                    break;
            }
        }
    }
}
