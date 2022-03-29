using System;

namespace ZyGames.Framework.Services.Messaging
{
    [Serializable]
    public class Message
    {
        public Guid Id { get; internal set; }

        public Address SendingSilo { get; internal set; }

        public Identity SendingId { get; internal set; }

        public Address TargetSilo { get; internal set; }

        public Identity TargetId { get; internal set; }

        public Directions Direction { get; internal set; }

        public ResponseTypes Result { get; internal set; }

        public RejectionTypes RejectionType { get; internal set; }

        public object Body { get; internal set; }

        public Message CreateResponseMessage(object obj)
        {
            var message = new Message();
            message.Id = Id;
            message.TargetSilo = SendingSilo;
            message.TargetId = SendingId;
            message.SendingSilo = TargetSilo;
            message.SendingId = TargetId;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Success;
            message.Body = obj;
            return message;
        }

        public Message CreateErrorMessage(Exception exception)
        {
            var message = new Message();
            message.Id = Id;
            message.TargetSilo = SendingSilo;
            message.TargetId = SendingId;
            message.SendingSilo = TargetSilo;
            message.SendingId = TargetId;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Error;
            message.Body = exception;
            return message;
        }

        public Message CreateRejectionMessage(RejectionTypes rejectionType)
        {
            var message = new Message();
            message.Id = Id;
            message.TargetSilo = SendingSilo;
            message.TargetId = SendingId;
            message.SendingSilo = TargetSilo;
            message.SendingId = TargetId;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Rejection;
            message.RejectionType = rejectionType;
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
            Rejection
        }

        public enum RejectionTypes : byte
        {
            Transient,
            Overloaded,
            DuplicateRequest,
            Unrecoverable,
            GatewayTooBusy,
            CacheInvalidation
        }
    }
}
