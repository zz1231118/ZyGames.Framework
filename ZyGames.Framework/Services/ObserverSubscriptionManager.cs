using System;
using System.Collections.Generic;

namespace ZyGames.Framework.Services
{
    public class ObserverSubscriptionManager<TObserver>
        where TObserver : IServiceObserver
    {
        private readonly List<TObserver> observers = new List<TObserver>();

        public IReadOnlyCollection<TObserver> Observers => observers;

        public void Subscribe(TObserver observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public bool Unsubscribe(TObserver observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            return observers.Remove(observer);
        }

        public bool Contains(TObserver observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            return observers.Contains(observer);
        }

        public void Clear()
        {
            observers.Clear();
        }

        public void Notify(Action<TObserver> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (var observer in observers)
            {
                action(observer);
            }
        }
    }
}
