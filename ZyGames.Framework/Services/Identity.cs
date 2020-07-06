using System;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public sealed class Identity : IEquatable<Identity>
    {
        private readonly Guid uniqueKey;
        private readonly Categories category;

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
            if (left is object && right is object) return left.uniqueKey == right.uniqueKey && left.category == right.category;
            else return ReferenceEquals(left, right);
        }

        public static bool operator !=(Identity left, Identity right)
        {
            if (left is object && right is object) return left.uniqueKey != right.uniqueKey || left.category != right.category;
            else return !ReferenceEquals(left, right);
        }

        internal static Identity NewIdentity(Categories category)
        {
            return new Identity(Guid.NewGuid(), category);
        }

        public static Identity NewIdentity()
        {
            return new Identity(Guid.NewGuid(), Categories.Service);
        }

        public bool Equals(Identity other)
        {
            return other != null && other.uniqueKey == uniqueKey && other.category == category;
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
