using System;
using System.Net;
using System.Text.RegularExpressions;

namespace ZyGames.Framework.Services
{
    [Serializable]
    public sealed class SlioAddress : IEquatable<SlioAddress>
    {
        private const string IPv4Pattern = @"^(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))(\.(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))){3}$";
        private const string IPv6Pattern = @"^[\da-fA-F]{1,4}(:[\da-fA-F]{1,4}){7}$";
        private const string IPv4EndpointPattern = @"^(?<host>(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))(\.(\d|[1-9]\d|1\d\d|2([0-4]\d|5[0-5]))){3})\:(?<port>\d+)$";
        private const string IPv6EndpointPattern = @"^(?<host>[\da-fA-F]{1,4}(:[\da-fA-F]{1,4}){7})\:(?<port>\d+)$";
        public static readonly SlioAddress None = new SlioAddress(IPAddress.Any.ToString(), 0);

        public SlioAddress(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var match = Regex.Match(text, IPv4EndpointPattern);
            if (!match.Success)
            {
                match = Regex.Match(text, IPv6EndpointPattern);
                if (!match.Success) throw new ArgumentException(text);
            }

            Host = match.Groups["host"].Value;
            Port = ushort.Parse(match.Groups["port"].Value);
        }

        public SlioAddress(string host, ushort port)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (!Regex.IsMatch(host, IPv4Pattern) && !Regex.IsMatch(host, IPv6Pattern))
                throw new ArgumentException(nameof(host));

            Host = host;
            Port = port;
        }

        public string Host { get; }

        public ushort Port { get; }

        public IPAddress Address => IPAddress.Parse(Host);

        public EndPoint EndPoint => new IPEndPoint(IPAddress.Parse(Host), Port);

        public static bool operator ==(SlioAddress left, SlioAddress right)
        {
            if (left is object && right is object) return left.Host == right.Host && left.Port == right.Port;
            else return ReferenceEquals(left, right);
        }

        public static bool operator !=(SlioAddress left, SlioAddress right)
        {
            if (left is object && right is object) return left.Host != right.Host || left.Port != right.Port;
            else return !ReferenceEquals(left, right);
        }

        public bool Equals(SlioAddress other)
        {
            return other != null && other.Host == Host && other.Port == Port;
        }

        public override bool Equals(object obj)
        {
            return obj is SlioAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Host.GetHashCode() << 16) ^ Port.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }
    }
}
