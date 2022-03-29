using System;
using ZyGames.Framework.Services.Lifecycle;
using ZyGames.Framework.Services.Dashboard.Metrics;
using Framework.Injection;

namespace ZyGames.Framework.Services.Dashboard
{
    public static class ServiceHostBuilderExtensions
    {
        public static ServiceHostBuilder UseDashboard(this ServiceHostBuilder source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            source.AddComponent<IServiceProfiler, ServiceProfiler>();
            source.AddComponent(p => (ILifecycleParticipant<IServiceHostLifecycle>)p.Required<IServiceProfiler>());
            source.AddComponent<IServiceCallFilter, ServiceProfilerFilter>();
            return source;
        }

        public static ServiceHostBuilder AddDashboard(this ServiceHostBuilder source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            source.AddService<IDashboardService, DashboardService>();
            return source;
        }
    }
}
