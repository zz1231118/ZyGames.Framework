using System;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;
using ZyGames.Framework.Remote.Networking;
using ZyGames.Framework.Remote.Options;

namespace ZyGames.Framework.Remote
{
    public class ServiceReferenceRuntime
    {
        private readonly ClientRuntime clientRuntime;
        private readonly ClientOptions clientOptions;

        internal ServiceReferenceRuntime(IServiceProvider serviceProvider, ClientRuntime clientRuntime)
        {
            this.clientRuntime = clientRuntime;
            this.clientOptions = serviceProvider.GetService<ClientOptions>() ?? ClientOptions.Default;
        }

        public void InvokeMethod(ServiceReference reference, int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            var request = new MethodInvokeRequest();
            request.MethodId = methodId;
            request.Arguments = arguments;

            var message = new Message();
            message.Guid = Guid.NewGuid();
            message.Target = reference.InterfaceId;
            message.BodyObject = request;
            message.Direction = options.HasFlag(InvokeMethodOptions.OneWay) ? Message.Directions.OneWay : Message.Directions.Request;
            if (clientOptions.Credentials != null)
            {
                message.Authorization = clientOptions.Credentials.Create();
            }
            if (options.HasFlag(InvokeMethodOptions.OneWay))
            {
                clientRuntime.SendMessage(message);
                return;
            }

            clientRuntime.SendRequest(message, clientOptions.RequestTimeout);
        }

        public T InvokeMethod<T>(ServiceReference reference, int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            var request = new MethodInvokeRequest();
            request.MethodId = methodId;
            request.Arguments = arguments;

            var message = new Message();
            message.Guid = Guid.NewGuid();
            message.Target = reference.InterfaceId;
            message.BodyObject = request;
            message.Direction = Message.Directions.Request;
            if (clientOptions.Credentials != null)
            {
                message.Authorization = clientOptions.Credentials.Create();
            }

            return (T)clientRuntime.SendRequest(message, clientOptions.RequestTimeout);
        }
    }
}
