using System;
using System.Threading;
using Framework.Log;

namespace ZyGames.Framework.Services.Lifecycle
{
    internal class ServiceHostLifecycle : LifecycleObservable, IServiceHostLifecycle
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public ServiceHostLifecycle()
            : base(Logger.GetLogger<ServiceHostLifecycle>())
        { }

        public ServiceHostStatus Status
        {
            get
            {
                switch (State.GetValueOrDefault())
                {
                    case Lifecycles.State.ServiceHost.Starting: return ServiceHostStatus.Joining;
                    case Lifecycles.State.ServiceHost.Started: return ServiceHostStatus.Started;
                    case Lifecycles.State.ServiceHost.Stopped: return ServiceHostStatus.Stopped;
                    default: return ServiceHostStatus.None;
                }
            }
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        public IDisposable WithStarted(string observerName, Action<CancellationToken> observer)
        {
            return base.Subscribe(observerName, Lifecycles.Stage.User, (token, state) =>
            {
                switch (state)
                {
                    case Lifecycles.State.ServiceHost.Started:
                        observer(token);
                        break;
                }
            });
        }

        public IDisposable WithStopped(string observerName, Action<CancellationToken> observer)
        {
            return base.Subscribe(observerName, Lifecycles.Stage.User, (Action<CancellationToken, int>)((token, state) =>
            {
                switch (state)
                {
                    case Lifecycles.State.ServiceHost.Stopped:
                        observer(token);
                        break;
                }
            }));
        }
    }
}
