using System.Reflection;

namespace ZyGames.Framework.Services
{
    public interface IServiceCallContext
    {
        IAddressable Service { get; }

        MethodInfo InterfaceMethod { get; }

        MethodInfo ImplementationMethod { get; }

        object[] Arguments { get; }

        object Result { get; set; }

        void Invoke();
    }
}
