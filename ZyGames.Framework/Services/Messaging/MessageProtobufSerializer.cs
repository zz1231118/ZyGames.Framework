using System;
using System.Runtime.Serialization.Formatters.Binary;
using Framework.IO;
using Framework.Runtime.Serialization.Protobuf;

namespace ZyGames.Framework.Services.Messaging
{
    internal class MessageProtobufSerializer : IMessageSerializer
    {
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        public byte[] Serialize(Message message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            using (var ms = recyclableMemoryStreamManager.GetStream(nameof(MessageProtobufSerializer)))
            {
                using (var writer = new ProtoWriter(ms, true))
                {
                    writer.WriteField(1, FieldType.Binary);
                    writer.WriteBytes(message.Id.ToByteArray());
                    if (message.SendingSilo != null)
                    {
                        writer.WriteField(2, FieldType.StartGroup);
                        var sendingSiloToken = writer.StartGroup();
                        message.SendingSilo.WriteTo(writer);
                        writer.EndGroup(sendingSiloToken);
                    }
                    if (message.SendingId != null)
                    {
                        writer.WriteField(3, FieldType.StartGroup);
                        var sendingIdToken = writer.StartGroup();
                        message.SendingId.WriteTo(writer);
                        writer.EndGroup(sendingIdToken);
                    }
                    if (message.TargetSilo != null)
                    {
                        writer.WriteField(4, FieldType.StartGroup);
                        var targetSiloToken = writer.StartGroup();
                        message.TargetSilo.WriteTo(writer);
                        writer.EndGroup(targetSiloToken);
                    }
                    if (message.TargetId != null)
                    {
                        writer.WriteField(5, FieldType.StartGroup);
                        var targetIdToken = writer.StartGroup();
                        message.TargetId.WriteTo(writer);
                        writer.EndGroup(targetIdToken);
                    }
                    if (message.Direction != Message.Directions.Request)
                    {
                        writer.WriteField(6, FieldType.Variant);
                        writer.WriteEnum(message.Direction);
                    }
                    if (message.Result != Message.ResponseTypes.Success)
                    {
                        writer.WriteField(7, FieldType.Variant);
                        writer.WriteEnum(message.Result);
                    }
                    if (message.RejectionType != Message.RejectionTypes.Transient)
                    {
                        writer.WriteField(8, FieldType.Variant);
                        writer.WriteEnum(message.RejectionType);
                    }
                    if (message.Body != null)
                    {
                        var formatter = new BinaryFormatter();
                        using (var oms = recyclableMemoryStreamManager.GetStream(nameof(MessageProtobufSerializer)))
                        {
                            formatter.Serialize(oms, message.Body);
                            writer.WriteField(9, FieldType.Binary);
                            writer.WriteBytes(oms.ToArray());
                        }
                    }
                }

                return ms.ToArray();
            }
        }

        public Message Deserialize(byte[] bytes)
        {
            var message = new Message();
            using (var ms = recyclableMemoryStreamManager.GetStream(nameof(MessageProtobufSerializer), bytes))
            {
                using (var reader = new ProtoReader(ms))
                {
                    while (reader.TryReadField(out var field))
                    {
                        switch (field)
                        {
                            case 1:
                                message.Id = new Guid(reader.ReadBytes());
                                break;
                            case 2:
                                var sendingSiloToken = reader.StartGroup();
                                message.SendingSilo = new Address();
                                message.SendingSilo.ReadFrom(reader);
                                reader.EndGroup(sendingSiloToken);
                                break;
                            case 3:
                                var sendingIdToken = reader.StartGroup();
                                message.SendingId = new Identity();
                                message.SendingId.ReadFrom(reader);
                                reader.EndGroup(sendingIdToken);
                                break;
                            case 4:
                                var targetSiloToken = reader.StartGroup();
                                message.TargetSilo = new Address();
                                message.TargetSilo.ReadFrom(reader);
                                reader.EndGroup(targetSiloToken);
                                break;
                            case 5:
                                var targetIdToken = reader.StartGroup();
                                message.TargetId = new Identity();
                                message.TargetId.ReadFrom(reader);
                                reader.EndGroup(targetIdToken);
                                break;
                            case 6:
                                message.Direction = reader.ReadEnum<Message.Directions>();
                                break;
                            case 7:
                                message.Result = reader.ReadEnum<Message.ResponseTypes>();
                                break;
                            case 8:
                                message.RejectionType = reader.ReadEnum<Message.RejectionTypes>();
                                break;
                            case 9:
                                var formatter = new BinaryFormatter();
                                using (var oms = recyclableMemoryStreamManager.GetStream(nameof(MessageProtobufSerializer), reader.ReadBytes()))
                                {
                                    message.Body = formatter.Deserialize(oms);
                                }
                                break;
                            default:
                                reader.SkipField();
                                break;
                        }
                    }
                    
                }
            }
            return message;
        }
    }
}
