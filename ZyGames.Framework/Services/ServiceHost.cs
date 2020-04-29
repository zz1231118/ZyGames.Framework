using System;
using System.Threading;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services
{
    public sealed class ServiceHost
    {
        private const int NoneSentinel = 0;
        private const int ActiveSentinel = 1;

        private readonly IServiceProvider serviceProvider;
        private readonly ServiceFactory serviceFactory;
        private readonly IServiceHostLifecycle lifecycle;
        private int isInRunning;

        internal ServiceHost(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.serviceFactory = serviceProvider.GetRequiredService<ServiceFactory>();
            this.lifecycle = serviceProvider.GetRequiredService<IServiceHostLifecycle>();
        }

        public ServiceHostStatus Status => lifecycle.Status;

        public IServiceProvider ServiceProvider => serviceProvider;

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
                lifecycle.NotifyObserver(Lifecycles.State.ServiceHost.Starting);
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
                lifecycle.NotifyObserver(Lifecycles.State.ServiceHost.Stopped);
            }
        }
    }
}
