using System;
using System.Collections.Generic;
using System.Linq;
using ZyGames.Framework.Services.Dashboard.Model;

namespace ZyGames.Framework.Services.Dashboard.Metrics.History
{
    internal class TraceHistory
    {
        private const int HistoryDurationInSeconds = 100;
        private const string Separator = ".";

        private readonly LinkedList<ServiceTraceEntry> history = new LinkedList<ServiceTraceEntry>();
        private readonly HashSet<ServiceTraceEntry> allMethods = new HashSet<ServiceTraceEntry>(ServiceTraceEqualityComparer.ByServiceAndMethodAndSilo);

        private static DateTime GetRetirementWindow(DateTime now)
        {
            return now.AddSeconds(-HistoryDurationInSeconds);
        }

        private static Dictionary<string, ServiceTraceEntry> GetTracings(IEnumerable<ServiceTraceEntry> traces)
        {
            var result = new Dictionary<string, ServiceTraceEntry>();
            var entries = traces.ToLookup(p => p.PeriodKey);
            var time = GetRetirementWindow(DateTime.UtcNow);
            for (int i = 0; i < HistoryDurationInSeconds; i++)
            {
                time = time.AddSeconds(1);
                var periodKey = time.ToPeriodString();
                var entry = new ServiceTraceEntry();
                entry.PeriodKey = periodKey;
                entry.Period = time;
                foreach (var trace in entries[periodKey])
                {
                    entry.Count += trace.Count;
                    entry.ExceptionCount += trace.ExceptionCount;
                    entry.ElapsedTime += trace.ElapsedTime;
                }

                result[periodKey] = entry;
            }
            return result;
        }

        public void Add(DateTime time, string address, ServiceTraceFragment[] serviceTrace)
        {
            var retirementWindow = GetRetirementWindow(time);
            var current = history.First;
            while (current != null)
            {
                var next = current.Next;
                if (current.Value.Period < retirementWindow)
                {
                    history.Remove(current);
                }

                current = next;
            }

            var periodKey = time.ToPeriodString();
            var added = new HashSet<ServiceTraceEntry>(ServiceTraceEqualityComparer.BySericeAndMethod);
            foreach (var entry in serviceTrace)
            {
                var newEntry = new ServiceTraceEntry();
                newEntry.PeriodKey = periodKey;
                newEntry.Period = time;
                newEntry.Address = address;
                newEntry.Service = entry.Service;
                newEntry.Method = entry.Method;
                newEntry.Count = entry.Count;
                newEntry.ExceptionCount = entry.ExceptionCount;
                newEntry.ElapsedTime = entry.ElapsedTime;
                if (!allMethods.Contains(newEntry))
                {
                    allMethods.Add(new ServiceTraceEntry() 
                    {
                        PeriodKey = periodKey,
                        Period = time,
                        Address = address,
                        Service = newEntry.Service,
                        Method = newEntry.Method,
                    });
                }
                if (added.Add(newEntry))
                {
                    history.AddLast(newEntry);
                }
            }
            foreach (var method in allMethods)
            {
                if (string.Equals(method.Address, address, StringComparison.OrdinalIgnoreCase) && added.Add(method))
                {
                    history.AddLast(method);
                }
            }
        }

        public Dictionary<string, ServiceTraceEntry> QueryAll()
        {
            return GetTracings(history);
        }

        public Dictionary<string, Dictionary<string, ServiceTraceEntry>> QueryService(string service)
        {
            var result = new Dictionary<string, Dictionary<string, ServiceTraceEntry>>();
            foreach (var group in history.Where(p => p.Service == service).GroupBy(p => (p.Service, p.Method)))
            {
                var methodKey = string.Join(Separator, group.Key.Service, group.Key.Method);
                result[methodKey] = GetTracings(group);
            }
            return result;
        }

        public List<ServiceAggregate> AggregateByService()
        {
            return history.GroupBy(p => p.Service).Select(p =>
            {
                var aggregate = new ServiceAggregate();
                aggregate.Service = p.Key;
                foreach (var item in p)
                {
                    aggregate.Count += item.Count;
                    aggregate.ExceptionCount += item.ExceptionCount;
                    aggregate.ElapsedTime += item.ElapsedTime;
                }
                return aggregate;
            }).ToList();
        }

        public List<SiloServiceAggregate> AggregateBySiloService()
        {
            return history.GroupBy(p => (p.Address, p.Service)).Select(p => 
            {
                var aggregate = new SiloServiceAggregate();
                aggregate.Address = p.Key.Address;
                aggregate.Service = p.Key.Service;
                foreach (var item in p)
                {
                    aggregate.Count += item.Count;
                    aggregate.ExceptionCount += item.ExceptionCount;
                    aggregate.ElapsedTime += item.ElapsedTime;
                }
                return aggregate;
            }).ToList();
        }

        public List<ServiceMethodAggregate> AggregateByServiceMethod()
        {
            return history.GroupBy(p => (p.Service, p.Method)).Select(p => 
            {
                var aggregate = new ServiceMethodAggregate();
                aggregate.Service = p.Key.Service;
                aggregate.Method = p.Key.Method;
                aggregate.NumberOfSamples = HistoryDurationInSeconds;
                foreach (var item in p)
                {
                    aggregate.Count += item.Count;
                    aggregate.ElapsedTime += item.ElapsedTime;
                    aggregate.ExceptionCount += item.ExceptionCount;
                }
                return aggregate;
            }).ToList();
        }
    }
}
