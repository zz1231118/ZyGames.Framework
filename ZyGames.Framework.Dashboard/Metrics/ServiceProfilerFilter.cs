using System;
using System.Collections.Concurrent;
using System.Reflection;
using ZyGames.Framework.Services.Runtime;

namespace ZyGames.Framework.Services.Dashboard.Metrics
{
    public class ServiceProfilerFilter : IServiceCallFilter
    {
        private readonly IServiceProfiler profiler;
        private readonly ConcurrentDictionary<MethodInfo, bool> shouldSkipMethods = new ConcurrentDictionary<MethodInfo, bool>();

        public ServiceProfilerFilter(IServiceProfiler profiler)
        {
            this.profiler = profiler;
        }

        private bool IsShouldSkipProfiling(IServiceCallContext context)
        {
            var method = context.InterfaceMethod;
            if (method == null)
            {
                return false;
            }
            if (!shouldSkipMethods.TryGetValue(method, out var shouldSkip))
            {
                var serviceType = context.Service.GetType();
                shouldSkip = serviceType.GetCustomAttribute<NoProfilingAttribute>() != null ||
                    method.GetCustomAttribute<NoProfilingAttribute>() != null;
                shouldSkipMethods[method] = shouldSkip;
            }
            return shouldSkip;
        }

        private void Track(IServiceCallContext context, ValueStopwatch stopwatch, bool isException)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var methodName = context.InterfaceMethod?.Name ?? "Unknown";
            profiler.Track(elapsedMs, context.Service.GetType(), methodName, isException);
        }

        public void Invoke(IServiceCallContext context)
        {
            if (IsShouldSkipProfiling(context))
            {
                context.Invoke();
                return;
            }

            var stopwatch = ValueStopwatch.StartNew();

            try
            {
                context.Invoke();
                Track(context, stopwatch, false);
            }
            catch (Exception)
            {
                Track(context, stopwatch, true);
                throw;
            }
        }
    }
}
