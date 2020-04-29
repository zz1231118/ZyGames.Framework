using System;
using System.Threading;
using Framework.Log;

namespace ZyGames.Framework.Services.Lifecycle
{
    internal class MembershipLifecycle : LifecycleObservable, IMembershipLifecycle
    {
        public MembershipLifecycle()
            : base(Logger.GetLogger<MembershipLifecycle>())
        { }

        public IDisposable WithChanged(string observerName, Action<CancellationToken> observer)
        {
            return base.Subscribe(observerName, Lifecycles.Stage.User, (Action<CancellationToken, int>)((token, state) =>
            {
                switch (state)
                {
                    case Lifecycles.State.Membership.Changed:
                        observer(token);
                        break;
                }
            }));
        }
    }
}
