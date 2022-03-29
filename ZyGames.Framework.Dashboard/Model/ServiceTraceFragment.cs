using System;

namespace ZyGames.Framework.Services.Dashboard.Model
{
    [Serializable]
    public class ServiceTraceFragment
    {
        public string Service { get; set; }

        public string Method { get; set; }

        public long Count { get; set; }

        public long ExceptionCount { get; set; }

        public double ElapsedTime { get; set; }
    }
}
