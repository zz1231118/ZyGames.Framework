using ZyGames.Framework.Services;

namespace SimpleFrameworkTest.Services
{
    public class WorldService : Service, IWorldService
    {
        public string World(string name)
        {
            return string.Format("world {0}", name);
        }
    }
}
