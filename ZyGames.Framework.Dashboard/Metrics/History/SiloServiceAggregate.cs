using System;

namespace ZyGames.Framework.Services.Dashboard.Metrics.History
{
    [Serializable]
    public class SiloServiceAggregate
    {
        public string Address { get; set; }

        public string Service { get; set; }

        public long Count { get; set; }

        public long ExceptionCount { get; set; }

        public double ElapsedTime { get; set; }
    }
}
