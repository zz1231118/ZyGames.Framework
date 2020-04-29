using System;
using ZyGames.Framework.Services.Messaging;

namespace ZyGames.Framework.Services.Runtime
{
    internal class ReferenceRuntime : IReferenceRuntime
    {
        private readonly MessageCenter messageCenter;

        internal ReferenceRuntime(MessageCenter messageCenter)
        {
            this.messageCenter = messageCenter;
        }

        public void InvokeMethod(Reference reference, int methodId, object[] arguments, InvokeMethodOptions options)
        {
            var sending = InvokerContext.Caller;
            var request = new InvokeMethodRequest();
            request.MethodId = methodId;
            request.Arguments = arguments;

            var message = new Message();
            message.Guid = Guid.NewGuid();
            if (sending != null)
            {
                message.SendingSlio = sending.Address;
                message.SendingId = sending.Identity;
            }
            message.TargetSlio = reference.Address;
            message.TargetId = reference.Identity;
            message.Direction = options.HasFlag(InvokeMethodOptions.OneWay) ? Message.Directions.OneWay : Message.Directions.Request;
            message.BodyObject = request;
            if (options.HasFlag(InvokeMethodOptions.OneWay))
            {
                messageCenter.SendMessage(message);
                return;
            }

            messageCenter.SendRequest(message);
        }

        public T InvokeMethod<T>(Reference reference, int methodId, object[] arguments, InvokeMethodOptions options)
        {
            var sending = InvokerContext.Caller;
            var request = new InvokeMethodRequest();
            request.MethodId = methodId;
            request.Arguments = arguments;

            var message = new Message();
            message.Guid = Guid.NewGuid();
            if (sending != null)
            {
                message.SendingSlio = sending.Address;
                message.SendingId = sending.Identity;
            }
            message.TargetSlio = reference.Address;
            message.TargetId = reference.Identity;
            message.Direction = options.HasFlag(InvokeMethodOptions.OneWay) ? Message.Directions.OneWay : Message.Directions.Request;
            message.BodyObject = request;
            return (T)messageCenter.SendRequest(message);
        }
    }
}
