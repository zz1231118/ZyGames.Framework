using System;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public sealed class Identity : IEquatable<Identity>
    {
        public readonly Guid UniqueKey;

        public readonly Categories Category;

        internal Identity(Guid uniqueKey, Categories category)
        {
            UniqueKey = uniqueKey;
            Category = category;
        }

        public static bool operator ==(Identity left, Identity right)
        {
            if (left is object && right is object) return left.UniqueKey == right.UniqueKey && left.Category == right.Category;
            else return ReferenceEquals(left, right);
        }

        public static bool operator !=(Identity left, Identity right)
        {
            if (left is object && right is object) return left.UniqueKey != right.UniqueKey || left.Category != right.Category;
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
            return other != null && other.UniqueKey == UniqueKey && other.Category == Category;
        }

        public override bool Equals(object obj)
        {
            return obj is Identity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (UniqueKey.GetHashCode() << 16) ^ Category.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{UniqueKey:{0} Category:{1}}}", UniqueKey, Category);
        }

        [Serializable]
        public enum Categories : byte
        {
            SystemTarget,
            Service,
        }
    }
}
