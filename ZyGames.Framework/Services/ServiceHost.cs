using System;
using System.Threading;
using Framework.Injection;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services
{
    public sealed class ServiceHost
    {
        private const int NoneSentinel = 0;
        private const int ActiveSentinel = 1;

        private readonly IContainer container;
        private readonly IServiceFactory serviceFactory;
        private readonly IServiceHostLifecycle lifecycle;
        private int isInRunning;

        internal ServiceHost(IContainer container)
        {
            this.container = container;
            this.serviceFactory = container.Required<IServiceFactory>();
            this.lifecycle = container.Required<IServiceHostLifecycle>();
            foreach (var participant in container.Repeated<ILifecycleParticipant<IServiceHostLifecycle>>())
            {
                participant.Participate(this.lifecycle);
            }
        }

        public ServiceHostStatus Status => lifecycle.Status;

        public IContainer Container => container;

        public IServiceFactory ServiceFactory => serviceFactory;

        public IServiceHostLifecycle Lifecycle => lifecycle;

        public void Start()
        {
            if (lifecycle.Status != ServiceHostStatus.None)
                throw new InvalidOperationException("invalid current status.");
            if (Interlocked.CompareExchange(ref isInRunning, ActiveSentinel, NoneSentinel) != NoneSentinel)
                throw new InvalidOperationException("activated");

            try
            {
                lifecycle.Notify(Lifecycles.State.ServiceHost.Starting);
            }
            catch
            {
                Interlocked.Exchange(ref isInRunning, NoneSentinel);
                throw;
            }
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref isInRunning, NoneSentinel) != NoneSentinel)
            {
                lifecycle.Notify(Lifecycles.State.ServiceHost.Stopped);
            }
        }
    }
}
