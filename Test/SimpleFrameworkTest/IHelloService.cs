using ZyGames.Framework.Services;

namespace SimpleFrameworkTest
{
    public interface IHelloService : IService
    {
        string Hello(string text);
    }
}
