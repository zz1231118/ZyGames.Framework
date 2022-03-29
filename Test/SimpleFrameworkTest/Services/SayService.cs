using ZyGames.Framework.Services;

namespace SimpleFrameworkTest.Services
{
    public class SayService : Service, ISayService
    {
        public string Say(string name)
        {
            var helloService = ServiceFactory.GetSingleService<IHelloService>();
            return string.Format("say {0}", helloService.Hello(name));
            //return $"hello: {name}";
        }
    }
}
