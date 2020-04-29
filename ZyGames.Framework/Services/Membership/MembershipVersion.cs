using System;
using System.Threading;

namespace ZyGames.Framework.Services.Membership
{
    [Serializable]
    public sealed class MembershipVersion : IComparable<MembershipVersion>, IEquatable<MembershipVersion>
    {
        private long version;

        public MembershipVersion()
        { }

        public MembershipVersion(long version)
        {
            this.version = version;
        }

        public MembershipVersion(MembershipVersion other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            version = other.version;
        }

        public long Version
        {
            get => version;
            set => version = value;
        }

        public static bool operator >(MembershipVersion left, MembershipVersion right)
        {
            return left is object && right is object && left.Version > right.Version;
        }

        public static bool operator >=(MembershipVersion left, MembershipVersion right)
        {
            if (left is object && right is object) return left.version >= right.version;
            else return ReferenceEquals(left, right);
        }

        public static bool operator ==(MembershipVersion left, MembershipVersion right)
        {
            if (left is object && right is object) return left.version == right.version;
            else return ReferenceEquals(left, right);
        }

        public static bool operator !=(MembershipVersion left, MembershipVersion right)
        {
            if (left is object && right is object) return left.version != right.version;
            else return !ReferenceEquals(left, right);
        }

        public static bool operator <=(MembershipVersion left, MembershipVersion right)
        {
            if (left is object && right is object) return left.version <= right.version;
            else return ReferenceEquals(left, right);
        }

        public static bool operator <(MembershipVersion left, MembershipVersion right)
        {
            return left is object && right is object && left.Version < right.Version;
        }

        public MembershipVersion Increment()
        {
            var value = Interlocked.Increment(ref version);
            return new MembershipVersion(value);
        }

        public int CompareTo(MembershipVersion other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Version.CompareTo(other.Version);
        }

        public bool Equals(MembershipVersion other)
        {
            return other != null && other.Version == Version;
        }

        public override bool Equals(object obj)
        {
            return obj is MembershipVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }
    }
}
