using ZyGames.Framework.Remote.Messaging;

namespace ZyGames.Framework.Remote
{
    public interface IServiceMethodInvoker
    {
        int InterfaceId { get; }

        object Invoke(IService service, MethodInvokeRequest request);
    }
}