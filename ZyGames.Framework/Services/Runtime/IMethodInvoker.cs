using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Runtime
{
    public interface IMethodInvoker
    {
        int InterfaceId { get; }

        string GetMethodName(InvokeMethodRequest request);

        object Invoke(IAddressable addressable, InvokeMethodRequest request);
    }
}
