using System;
using System.Threading;
using Framework.Injection;
using Framework.Log;
using ZyGames.Framework.Services.Directory;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Membership;
using ZyGames.Framework.Services.Networking;
using ZyGames.Framework.Services.Options;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services
{
    public sealed class GatewayMembershipService : SystemTarget, IGatewayMembershipService, ILifecycleObserver, IOptions<GatewayMembershipServiceOptions>
    {
        private readonly ILogger logger = Logger.GetLogger<GatewayMembershipService>();
        private GatewayMembershipServiceOptions membershipServiceOptions;
        private ActivationDirectory activationDirectory;
        private AddressableDirectory addressableDirectory;
        private MembershipManager membershipManager;
        private GatewayConnectionListener connectionListener;
        private IServiceHostLifecycle hostingLifecycle;
        private IClusterMembershipService clusterMembershipService;
        private Timer membershipCheckingUpdateTimer;
        private MembershipEntry membershipEntry;
        private bool isAlived;

        internal GatewayMembershipService()
        { }

        private MembershipTable CreateMembershipTable()
        {
            var membershipTable = activationDirectory.CreateMembershipTable();
            membershipTable.Entry = membershipEntry;
            return membershipTable;
        }

        private void MembershipCheckingUpdate()
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
                        hostingLifecycle.Notify(Lifecycles.State.ServiceHost.Started);
                    }

                    isAlived = true;
                }
            }
            catch (Exception ex)
            {
                isAlived = false;
                //hostingLifecycle.Notify(Lifecycles.State.ServiceHost.Starting);
                logger.Warn("{0}.{1} error:{2}", nameof(GatewayMembershipService), nameof(MembershipCheckingUpdate), ex);
            }
        }

        private void MembershipCheckingCallbacked(object obj)
        {
            var checkingUpdatePeriod = Timeout.InfiniteTimeSpan;
            membershipCheckingUpdateTimer.Change(checkingUpdatePeriod, checkingUpdatePeriod);
            InvokerContext.Caller = this;

            try
            {
                MembershipCheckingUpdate();
            }
            finally
            {
                InvokerContext.Caller = null;
            }

            checkingUpdatePeriod = membershipServiceOptions.MembershipCheckingUpdatePeriod;
            membershipCheckingUpdateTimer.Change(checkingUpdatePeriod, checkingUpdatePeriod);
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            membershipServiceOptions = Container.Required<GatewayMembershipServiceOptions>();
            activationDirectory = Container.Required<ActivationDirectory>();
            addressableDirectory = Container.Required<AddressableDirectory>();
            membershipManager = Container.Required<MembershipManager>();
            hostingLifecycle = Container.Required<IServiceHostLifecycle>();

            Address = membershipServiceOptions.OutsideAddress;
            Identity = Identity.NewIdentity(Identity.Categories.SystemTarget);

            membershipEntry = new MembershipEntry();
            membershipEntry.Identity = Identity;
            membershipEntry.Address = Address;

            clusterMembershipService = ServiceFactory.GetSystemTarget<IClusterMembershipService>(Constants.ClusterMembershipServiceIdentity, membershipServiceOptions.Cluster);
            var lifecycle = Container.Required<IDirectoryLifecycle>();
            lifecycle.Subscribe<GatewayMembershipService>(Lifecycles.Stage.System, this);
        }

        protected internal override void Start()
        {
            base.Start();
            connectionListener = new GatewayConnectionListener(Container, membershipServiceOptions);
            connectionListener.Start();

            MembershipCheckingUpdate();

            var checkingUpdatePeriod = membershipServiceOptions.MembershipCheckingUpdatePeriod;
            membershipCheckingUpdateTimer = new Timer(new TimerCallback(MembershipCheckingCallbacked), null, checkingUpdatePeriod, checkingUpdatePeriod);
        }

        protected internal override void Stop()
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

            membershipCheckingUpdateTimer.Dispose();
            connectionListener.Stop();
        }

        public void MembershipTableChanged(MembershipVersion version)
        {
            if (membershipManager.Version == null || membershipManager.Version < version)
            {
                MembershipSnapshot membershipSnapshot;

                try
                {
                    membershipSnapshot = clusterMembershipService.CreateMembershipSnapshot();
                }
                catch (Exception ex)
                {
                    logger.Warn("{0}.{1} error:{2}", nameof(GatewayMembershipService), nameof(IClusterMembershipService.CreateMembershipSnapshot), ex);
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
                        clusterMembershipService.MembershipTableChanged(membershipTable);
                    }
                    break;
            }
        }
    }
}
