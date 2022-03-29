using System.Collections.Generic;
using ZyGames.Framework.Services.Dashboard.Model;

namespace ZyGames.Framework.Services.Dashboard
{
    [ServiceContract("{CDA6AFB5-2779-412F-97BA-F462B4A8C4F5}")]
    public interface IDashboardService : IService
    {
        [OperationContract]
        void SubmitTracing(string address, ServiceTraceFragment[] serviceCallTime);

        [OperationContract]
        Dictionary<string, Dictionary<string, ServiceTraceEntry>> GetServiceTracing(string service);

        Dictionary<string, ServiceTraceEntry> GetClusterTracing();
    }
}
