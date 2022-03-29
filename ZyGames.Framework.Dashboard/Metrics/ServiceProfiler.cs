using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Framework.Log;
using ZyGames.Framework.Services.Dashboard.Model;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services.Dashboard.Metrics
{
    public sealed class ServiceProfiler : IServiceProfiler, ILifecycleParticipant<IServiceHostLifecycle>
    {
        private readonly ILogger<ServiceProfiler> logger = Logger.GetLogger<ServiceProfiler>();
        private readonly IServiceFactory serviceFactory;
        private volatile ConcurrentDictionary<string, ServiceTraceFragment> traces = new ConcurrentDictionary<string, ServiceTraceFragment>();
        private IDashboardService dashboardService;
        private Timer timer;

        public ServiceProfiler(IServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        private void ProcessStats(object obj)
        {
            var currentTraces = Interlocked.Exchange(ref traces, new ConcurrentDictionary<string, ServiceTraceFragment>());
            if (currentTraces.Count > 0)
            {
                var items = currentTraces.Values.ToArray();
                try
                {
                    if (dashboardService == null || !dashboardService.IsAlived)
                    {
                        dashboardService = serviceFactory.GetService<IDashboardService>();
                    }

                    dashboardService.SubmitTracing(string.Empty, items);
                }
                catch (Exception ex)
                {
                    logger.Warn("Exception thrown sending tracing to dashboard service: {0}", ex);
                }
            }
        }

        private void OnStarted(CancellationToken token)
        {
            timer = new Timer(new TimerCallback(ProcessStats), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void Participate(IServiceHostLifecycle lifecycle)
        {
            lifecycle.WithStarted(nameof(ServiceProfiler), OnStarted);
        }

        public void Track(double elapsedMs, Type serviceType, string methodName, bool failed = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var serviceName = serviceType.Name;
            var key = $"{serviceName}.{methodName}";
            var exceptionCount = failed ? 1 : 0;
            traces.AddOrUpdate(key, _ => 
            {
                var fragment = new ServiceTraceFragment();
                fragment.Service = serviceName;
                fragment.Method = methodName;
                fragment.Count = 1;
                fragment.ExceptionCount = exceptionCount;
                fragment.ElapsedTime = elapsedMs;
                return fragment;
            }, (_, last) => 
            {
                last.Count += 1;
                last.ElapsedTime += elapsedMs;
                if (failed)
                {
                    last.ExceptionCount += exceptionCount;
                }
                return last;
            });
        }
    }
}
