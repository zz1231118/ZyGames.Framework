using System;

namespace ZyGames.Framework.Services.Dashboard.Model
{
    [Serializable]
    public sealed class ServiceTraceEntry
    {
        public string PeriodKey { get; set; }

        public DateTime Period { get; set; }

        public string Address { get; set; }

        public string Service { get; set; }

        public string Method { get; set; }

        public long Count { get; set; }

        public long ExceptionCount { get; set; }

        public double ElapsedTime { get; set; }
    }
}
