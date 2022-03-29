namespace ZyGames.Framework.Services
{
    public interface IServiceCallFilter
    {
        void Invoke(IServiceCallContext context);
    }
}
