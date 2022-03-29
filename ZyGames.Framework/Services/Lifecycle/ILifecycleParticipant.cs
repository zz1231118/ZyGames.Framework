namespace ZyGames.Framework.Services.Lifecycle
{
    public interface ILifecycleParticipant<TLifecycleObservable>
        where TLifecycleObservable : ILifecycleObservable
    {
        void Participate(TLifecycleObservable lifecycle);
    }
}
