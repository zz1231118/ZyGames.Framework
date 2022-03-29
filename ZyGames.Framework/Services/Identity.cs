using System;
using Framework.Runtime.Serialization.Protobuf;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public sealed class Identity : IEquatable<Identity>, IMessage
    {
        private Guid uniqueKey;
        private Categories category;

        internal Identity()
        { }

        internal Identity(Guid uniqueKey, Categories category)
        {
            this.uniqueKey = uniqueKey;
            this.category = category;
        }

        public Identity(Guid uniqueKey)
        {
            this.uniqueKey = uniqueKey;
            this.category = Categories.Service;
        }

        internal Categories Category => category;

        public Guid UniqueKey => uniqueKey;

        public static bool operator ==(Identity left, Identity right)
        {
            return left is not null && right is not null
                ? left.uniqueKey == right.uniqueKey && left.category == right.category
                : ReferenceEquals(left, right);
        }

        public static bool operator !=(Identity left, Identity right)
        {
            return left is not null && right is not null
                ? left.uniqueKey != right.uniqueKey || left.category != right.category
                : !ReferenceEquals(left, right);
        }

        internal static Identity NewIdentity(Categories category)
        {
            return new Identity(Guid.NewGuid(), category);
        }

        public static Identity NewIdentity()
        {
            return new Identity(Guid.NewGuid(), Categories.Service);
        }

        public void ReadFrom(ProtoReader reader)
        {
            while (reader.TryReadField(out var field))
            {
                switch (field)
                {
                    case 1:
                        uniqueKey = new Guid(reader.ReadBytes());
                        break;
                    case 2:
                        category = reader.ReadEnum<Categories>();
                        break;
                    default:
                        reader.SkipField();
                        break;
                }
            }
        }

        public void WriteTo(ProtoWriter writer)
        {
            writer.WriteField(1, FieldType.Binary);
            writer.WriteBytes(uniqueKey.ToByteArray());

            writer.WriteField(2, FieldType.Variant);
            writer.WriteEnum(category);
        }

        public bool Equals(Identity other)
        {
            return other is not null && other.uniqueKey == uniqueKey && other.category == category;
        }

        public override bool Equals(object obj)
        {
            return obj is Identity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (uniqueKey.GetHashCode() << 16) ^ category.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{UniqueKey:{0} Category:{1}}}", uniqueKey, category);
        }

        [Serializable]
        public enum Categories : byte
        {
            SystemTarget,
            Service,
        }
    }
}
