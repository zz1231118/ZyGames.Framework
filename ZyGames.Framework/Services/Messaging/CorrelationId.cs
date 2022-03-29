using System;
using System.Threading;
using Framework.Runtime.Serialization.Protobuf;

namespace ZyGames.Framework.Services.Messaging
{
    [Serializable]
    public sealed class CorrelationId : IComparable<CorrelationId>, IEquatable<CorrelationId>, IMessage
    {
        private static long nextTo = 1;
        private long value;

        internal CorrelationId()
        { }

        internal CorrelationId(long value)
        {
            this.value = value;
        }

        public static bool operator ==(CorrelationId left, CorrelationId right)
        {
            return left is not null && right is not null 
                ? left.value == right.value 
                : ReferenceEquals(left, right);
        }

        public static bool operator !=(CorrelationId left, CorrelationId right)
        {
            return left is not null && right is not null 
                ? left.value != right.value 
                : !ReferenceEquals(left, right);
        }

        public static CorrelationId NewId()
        {
            var value = Interlocked.Increment(ref nextTo);
            return new CorrelationId(value);
        }

        public long ToInt64()
        {
            return value;
        }

        public void ReadFrom(ProtoReader reader)
        {
            while (reader.TryReadField(out var field))
            {
                switch (field)
                {
                    case 1:
                        value = reader.ReadInt64();
                        break;
                    default:
                        reader.SkipField();
                        break;
                }
            }
        }

        public void WriteTo(ProtoWriter writer)
        {
            writer.WriteField(1, FieldType.Variant);
            writer.WriteInt64(value);
        }

        public int CompareTo(CorrelationId other)
        {
            return other is null ? 1 : value.CompareTo(other.value);
        }

        public bool Equals(CorrelationId other)
        {
            return other is not null && other.value == value;
        }

        public override bool Equals(object obj)
        {
            return obj is CorrelationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
