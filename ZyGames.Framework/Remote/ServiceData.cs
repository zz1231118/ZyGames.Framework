using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote
{
    internal class ServiceData
    {
        public ServiceData(IService service, IServiceMethodInvoker invoker)
        {
            Service = service;
            Invoker = invoker;
        }

        public IService Service { get; }

        public IServiceMethodInvoker Invoker { get; }

        public object Invoke(MethodInvokeRequest request)
        {
            return Invoker.Invoke(Service, request);
        }
    }
}
