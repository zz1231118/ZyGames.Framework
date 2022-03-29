using System;
using System.Net;
using System.Threading;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services
{
    internal static class Extensions
    {
        public static EndPoint GetEndPoint(this Address source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new IPEndPoint(IPAddress.Parse(source.Host), source.Port);
        }

        public static void Subscribe<T>(this ILifecycleObservable observable, int stage, ILifecycleObserver observer)
        {
            if (observable == null)
                throw new ArgumentNullException(nameof(observable));

            observable.Subscribe(typeof(T).Name, stage, observer);
        }

        public static void Subscribe<T>(this ILifecycleObservable observable, int stage, Action<CancellationToken, int> observer)
        {
            if (observable == null)
                throw new ArgumentNullException(nameof(observable));

            observable.Subscribe(typeof(T).Name, stage, observer);
        }

        public static void Notify(this ILifecycleObservable observable, int state)
        {
            if (observable == null)
                throw new ArgumentNullException(nameof(observable));

            observable.Notify(CancellationToken.None, state);
        }
    }
}
