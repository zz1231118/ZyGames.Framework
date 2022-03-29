using System;
using System.Net;
using System.Text.RegularExpressions;
using Framework.Runtime.Serialization.Protobuf;

namespace ZyGames.Framework.Services
{
    /// <summary>
    /// 地址
    /// </summary>
    [Serializable]
    public sealed class Address : IEquatable<Address>, IMessage
    {
        private const string IPv4Pattern = @"^(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))(\.(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))){3}$";
        private const string IPv6Pattern = @"^[\da-fA-F]{1,4}(:[\da-fA-F]{1,4}){7}$";
        private const string IPv4EndpointPattern = @"^(?<host>(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))(\.(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))){3})\:(?<port>\d+)$";
        private const string IPv6EndpointPattern = @"^(?<host>[\da-fA-F]{1,4}(:[\da-fA-F]{1,4}){7})\:(?<port>\d+)$";

        /// <inheritdoc />
        public static readonly Address None = new Address(IPAddress.Any.ToString(), 0);

        private string host;
        private ushort port;

        private Address(string host, ushort port, bool _)
        {
            this.host = host;
            this.port = port;
        }

        internal Address()
        { }

        /// <inheritdoc />
        public Address(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var match = Regex.Match(text, IPv4EndpointPattern);
            if (!match.Success)
            {
                match = Regex.Match(text, IPv6EndpointPattern);
                if (!match.Success) throw new ArgumentException(text);
            }

            host = match.Groups["host"].Value;
            port = ushort.Parse(match.Groups["port"].Value);
        }

        /// <inheritdoc />
        public Address(string host, ushort port)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (!Regex.IsMatch(host, IPv4Pattern) && !Regex.IsMatch(host, IPv6Pattern))
                throw new ArgumentException(nameof(host));

            this.host = host;
            this.port = port;
        }

        /// <summary>
        /// 地址
        /// </summary>
        public string Host => host;

        /// <summary>
        /// 端口
        /// </summary>
        public ushort Port => port;

        /// <inheritdoc />
        public static bool operator ==(Address left, Address right)
        {
            return left is not null && right is not null 
                ? left.host == right.host && left.port == right.port 
                : ReferenceEquals(left, right);
        }

        /// <inheritdoc />
        public static bool operator !=(Address left, Address right)
        {
            return left is not null && right is not null
                ? left.host != right.host || left.port != right.port
                : !ReferenceEquals(left, right);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Address Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var match = Regex.Match(text, IPv4EndpointPattern);
            if (!match.Success)
            {
                match = Regex.Match(text, IPv6EndpointPattern);
                if (!match.Success) throw new ArgumentException(text);
            }

            var host = match.Groups["host"].Value;
            var port = ushort.Parse(match.Groups["port"].Value);
            return new Address(host, port, true);
        }

        /// <inheritdoc />
        /// <exception cref="System.ArgumentNullException"></exception>
        public static bool TryParse(string text, out Address address)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var match = Regex.Match(text, IPv4EndpointPattern);
            if (!match.Success)
            {
                match = Regex.Match(text, IPv6EndpointPattern);
                if (!match.Success)
                {
                    address = null;
                    return false;
                }
            }

            var host = match.Groups["host"].Value;
            var port = ushort.Parse(match.Groups["port"].Value);
            address = new Address(host, port, true);
            return true;
        }

        public void ReadFrom(ProtoReader reader)
        {
            while (reader.TryReadField(out var field))
            {
                switch (field)
                {
                    case 1:
                        host = reader.ReadString();
                        break;
                    case 2:
                        port = reader.ReadUInt16();
                        break;
                    default:
                        reader.SkipField();
                        break;
                }
            }
        }

        public void WriteTo(ProtoWriter writer)
        {
            writer.WriteField(1, FieldType.String);
            writer.WriteString(host);

            writer.WriteField(2, FieldType.Variant);
            writer.WriteUInt16(port);
        }

        public bool Equals(Address other)
        {
            return other is not null && other.host == host && other.port == port;
        }

        public override bool Equals(object obj)
        {
            return obj is Address other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (host.GetHashCode() << 16) ^ port.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", host, port);
        }
    }
}
