using System;
using ZyGames.Framework.Injection;
using ZyGames.Framework.Remote.Messaging;
using ZyGames.Framework.Remote.Networking;
using ZyGames.Framework.Remote.Options;

namespace ZyGames.Framework.Remote
{
    public class MessageDispatcher
    {
        private readonly ServiceOptions serviceOptions;
        private readonly ServiceDirectory serviceDirectory;

        internal MessageDispatcher(IServiceProvider serviceProvider)
        {
            this.serviceOptions = serviceProvider.GetService<ServiceOptions>();
            this.serviceDirectory = serviceProvider.GetRequiredService<ServiceDirectory>();
        }

        public void Dispatch(Connection connection, Message message)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            switch (message.Direction)
            {
                case Message.Directions.Request:
                case Message.Directions.OneWay:
                    if (serviceOptions?.Credentials != null)
                    {
                        if (message.Authorization == null || !serviceOptions.Credentials.Authenticate(message.Authorization))
                        {
                            var faultedMessage = message.CreateErrorMessage(new InvalidOperationException("authorizate failed."));
                            connection.SendMessage(faultedMessage);
                            return;
                        }
                    }
                    break;
                default:
                    {
                        var faultedMessage = message.CreateErrorMessage(new InvalidOperationException(string.Format("invalid direction: {0}", message.Direction)));
                        connection.SendMessage(faultedMessage);
                    }
                    return;
            }
            var target = serviceDirectory.FindTarget(message.Target);
            if (target == null)
            {
                var faultedMessage = message.CreateErrorMessage(new InvalidOperationException(string.Format("service:{0} not found.", message.Target)));
                connection.SendMessage(faultedMessage);
                return;
            }

            object result;

            try
            {
                var methodInvokeRequest = (MethodInvokeRequest)message.BodyObject;
                result = target.Invoke(methodInvokeRequest);
            }
            catch (Exception ex)
            {
                var faultedMessage = message.CreateErrorMessage(ex);
                connection.SendMessage(faultedMessage);
                return;
            }
            if (message.Direction == Message.Directions.Request)
            {
                var response = message.CreateResponseMessage(result);
                connection.SendMessage(response);
            }
        }
    }
}
