using ZyGames.Framework.Services;

namespace SimpleFrameworkTest.Services
{
    public class HelloService : Service, IHelloService
    {
        public string Hello(string name)
        {
            var worldService = ServiceFactory.GetSingleService<IWorldService>();
            return string.Format("hello {0}", worldService.World(name));
        }
    }
}
