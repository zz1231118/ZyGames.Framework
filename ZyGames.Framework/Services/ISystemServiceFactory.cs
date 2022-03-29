namespace ZyGames.Framework.Services
{
    public interface ISystemServiceFactory : IServiceFactory
    {
        TSystemTargetInterface GetSystemTarget<TSystemTargetInterface>(Identity identity, Address destination)
            where TSystemTargetInterface : ISystemTarget;
    }
}
