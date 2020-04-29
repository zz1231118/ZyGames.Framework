using System;

namespace ZyGames.Framework.Services.Messaging
{
    [Serializable]
    public class Message
    {
        public Guid Guid { get; internal set; }

        public SlioAddress SendingSlio { get; internal set; }

        public Identity SendingId { get; internal set; }

        public SlioAddress TargetSlio { get; internal set; }

        public Identity TargetId { get; internal set; }

        public Directions Direction { get; internal set; }

        public ResponseTypes Result { get; internal set; }

        public RejectionTypes RejectionType { get; internal set; }

        public object BodyObject { get; internal set; }

        public Message CreateResponseMessage(object obj)
        {
            var message = new Message();
            message.Guid = Guid;
            message.TargetSlio = SendingSlio;
            message.TargetId = SendingId;
            message.SendingSlio = TargetSlio;
            message.SendingId = TargetId;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Success;
            message.BodyObject = obj;
            return message;
        }

        public Message CreateErrorMessage(Exception exception)
        {
            var message = new Message();
            message.Guid = Guid;
            message.TargetSlio = SendingSlio;
            message.TargetId = SendingId;
            message.SendingSlio = TargetSlio;
            message.SendingId = TargetId;
            message.Direction = Directions.Response;
            message.Result = ResponseTypes.Error;
            message.BodyObject = exception;
            return message;
        }

        public Message CreateRejectionMessage(RejectionTypes rejectionType)
        {
            var message = new Message();
            message.Guid = Guid;
            message.TargetSlio = SendingSlio;
            message.TargetId = SendingId;
            message.SendingSlio = TargetSlio;
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
