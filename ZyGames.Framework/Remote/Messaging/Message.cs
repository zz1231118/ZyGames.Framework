using System;
using ZyGames.Framework.Security;

namespace ZyGames.Framework.Remote.Messaging
{
    [Serializable]
    public class Message
    {
        public Guid Guid { get; internal set; }

        public int Sending { get; internal set; }

        public int Target { get; internal set; }

        public Directions Direction { get; internal set; }

        public ResponseTypes Result { get; internal set; }

        public IAuthorization Authorization { get; internal set; }

        public object BodyObject { get; internal set; }

        internal Message CreateErrorMessage(Exception exception)
        {
            var message = new Message();
            message.Guid = Guid;
            message.Sending = Target;
            message.Target = Sending;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Error;
            message.BodyObject = exception;
            return message;
        }

        internal Message CreateResponseMessage(object obj)
        {
            var message = new Message();
            message.Guid = Guid;
            message.Sending = Target;
            message.Target = Sending;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Success;
            message.BodyObject = obj;
            return message;
        }

        public enum Directions : byte
        {
            Request,
            Response,
            OneWay,
        }

        public enum ResponseTypes : byte
        {
            Success,
            Error,
            Rejection,
        }
    }
}
