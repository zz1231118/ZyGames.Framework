using ZyGames.Framework.Services;

namespace SimpleFrameworkTest
{
    public interface ISayService : IService
    {
        string Say(string name);
    }
}
