using System;
using System.Collections.Generic;
using ZyGames.Framework.Services.Dashboard.Metrics.History;
using ZyGames.Framework.Services.Dashboard.Model;

namespace ZyGames.Framework.Services.Dashboard
{
    public sealed class DashboardService : Service, IDashboardService
    {
        private readonly TraceHistory history = new TraceHistory();

        public void SubmitTracing(string address, ServiceTraceFragment[] serviceCallTime)
        {
            history.Add(DateTime.UtcNow, address, serviceCallTime);
        }

        public Dictionary<string, Dictionary<string, ServiceTraceEntry>> GetServiceTracing(string service)
        {
            return history.QueryService(service);
        }

        public Dictionary<string, ServiceTraceEntry> GetClusterTracing()
        {
            return history.QueryAll();
        }
    }
}
