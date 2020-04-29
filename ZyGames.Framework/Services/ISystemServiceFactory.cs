namespace ZyGames.Framework.Services
{
    public interface ISystemServiceFactory : IServiceFactory
    {
        TSystemTargetInterface GetSystemTarget<TSystemTargetInterface>(Identity identity, SlioAddress destination)
            where TSystemTargetInterface : ISystemTarget;
    }
}
