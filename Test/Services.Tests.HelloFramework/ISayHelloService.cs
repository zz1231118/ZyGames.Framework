using ZyGames.Framework.Services;

namespace Services.Tests.HelloFramework
{
    public interface ISayHelloService : IService
    {
        string SayHello(string name);
    }
}
