using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Framework.Log;

namespace ZyGames.Framework.Services.Lifecycle
{
    internal class LifecycleObservable : ILifecycleObservable
    {
        private readonly ILogger logger;
        private readonly List<OrderedObserver> subscribers = new List<OrderedObserver>();
        private int? highStage;
        private int? nextState;

        public LifecycleObservable(ILogger logger)
        {
            this.logger = logger;
        }

        public int? State => nextState;

        public virtual IDisposable Subscribe(string observerName, int stage, ILifecycleObserver observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            if (highStage.HasValue)
                throw new InvalidOperationException("Lifecycle has already been started.");

            var orderedObserver = new LifecycleOrderedObserver(observerName, stage, observer);
            subscribers.Add(orderedObserver);
            return new Disposable(() => subscribers.Remove(orderedObserver));
        }

        public virtual IDisposable Subscribe(string observerName, int stage, Action<CancellationToken, int> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            if (highStage.HasValue)
                throw new InvalidOperationException("Lifecycle has already been started.");

            var orderedObserver = new ActionOrderedObserver(observerName, stage, observer);
            subscribers.Add(orderedObserver);
            return new Disposable(() => subscribers.Remove(orderedObserver));
        }

        public void NotifyObserver(CancellationToken token, int state)
        {
            string observerName = null;

            try
            {
                foreach (var observerGroup in subscribers.GroupBy(orderedObserver => orderedObserver.Stage).OrderBy(group => group.Key))
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new LifecycleCanceledException("Lifecycle start canceled by request");
                    }

                    highStage = observerGroup.Key;
                    foreach (var orderedObserver in observerGroup)
                    {
                        observerName = orderedObserver.Name;
                        orderedObserver.Notify(token, state);
                    }
                }
                if (nextState == null || state > nextState.Value)
                {
                    nextState = state;
                }
            }
            catch (Exception ex) when (!(ex is LifecycleCanceledException))
            {
                logger?.Error("Lifecycle start canceled due to errors {0} at stage {1}: {2}", observerName, highStage.GetValueOrDefault(), ex);
                throw;
            }
        }

        public void NotifyObserver(int state)
        {
            NotifyObserver(CancellationToken.None, state);
        }

        class Disposable : IDisposable
        {
            private readonly Action dispose;

            public Disposable(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                dispose();
            }
        }

        abstract class OrderedObserver
        {
            public OrderedObserver(string name, int stage)
            {
                Name = name;
                Stage = stage;
            }

            public string Name { get; }

            public int Stage { get; }

            public abstract void Notify(CancellationToken token, int state);
        }

        class LifecycleOrderedObserver : OrderedObserver
        {
            public LifecycleOrderedObserver(string name, int stage, ILifecycleObserver observer)
                : base(name, stage)
            {
                Observer = observer;
            }

            public ILifecycleObserver Observer { get; }

            public override void Notify(CancellationToken token, int state)
            {
                Observer.Notify(token, state);
            }
        }

        class ActionOrderedObserver : OrderedObserver
        {
            public ActionOrderedObserver(string name, int stage, Action<CancellationToken, int> observer)
                : base(name, stage)
            {
                Observer = observer;
            }

            public Action<CancellationToken, int> Observer { get; }

            public override void Notify(CancellationToken token, int state)
            {
                Observer.Invoke(token, state);
            }
        }
    }
}
