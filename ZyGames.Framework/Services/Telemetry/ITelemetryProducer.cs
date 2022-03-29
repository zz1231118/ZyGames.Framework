using System;
using System.Collections.Generic;

namespace ZyGames.Framework.Services.Telemetry
{
    public interface ITelemetryProducer
    {
        /// <summary>Send a metric value to the registered telemetry consumers.</summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="properties">Named string values you can use to classify and filter metrics.</param>
        void TrackMetric(string name, double value, IDictionary<string, string> properties = null);

        /// <summary>Send a metric value to the registered telemetry consumers.</summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="properties">Named string values you can use to classify and filter metrics.</param>
        void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null);
    }
}
