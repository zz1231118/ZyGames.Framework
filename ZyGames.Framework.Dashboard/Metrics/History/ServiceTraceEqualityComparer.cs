using System;
using System.Collections.Generic;
using ZyGames.Framework.Services.Dashboard.Model;

namespace ZyGames.Framework.Services.Dashboard.Metrics.History
{
    public sealed class ServiceTraceEqualityComparer : IEqualityComparer<ServiceTraceEntry>
    {
        public static readonly ServiceTraceEqualityComparer BySericeAndMethod = new ServiceTraceEqualityComparer(false);
        public static readonly ServiceTraceEqualityComparer ByServiceAndMethodAndSilo = new ServiceTraceEqualityComparer(true);

        private readonly bool withAddress;

        public ServiceTraceEqualityComparer(bool withAddress)
        {
            this.withAddress = withAddress;
        }

        public bool Equals(ServiceTraceEntry x, ServiceTraceEntry y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x is null || y is null)
            {
                return false;
            }
            var isEquals = 
                string.Equals(x.Service, y.Service, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Method, y.Method, StringComparison.OrdinalIgnoreCase);
            if (withAddress)
            {
                isEquals &= string.Equals(x.Address, y.Address, StringComparison.OrdinalIgnoreCase);
            }
            return isEquals;
        }

        public int GetHashCode(ServiceTraceEntry obj)
        {
            if (obj is null)
            {
                return 0;
            }
            var hashCode = 17;
            if (obj.Service != null)
            {
                hashCode = hashCode * 23 + obj.Service.GetHashCode();
            }
            if (obj.Method != null)
            {
                hashCode = hashCode * 23 + obj.Method.GetHashCode();
            }
            if (obj.Address != null && withAddress)
            {
                hashCode = hashCode * 23 + obj.Address.GetHashCode();
            }

            return hashCode;
        }
    }
}
