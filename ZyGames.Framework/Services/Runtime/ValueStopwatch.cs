using System;
using System.Diagnostics;

namespace ZyGames.Framework.Services.Runtime
{
    public struct ValueStopwatch
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        private long value;

        private ValueStopwatch(long timestamp)
        {
            value = timestamp;
        }

        public bool IsRunning => value > 0;

        public TimeSpan Elapsed => TimeSpan.FromTicks(ElapsedTicks);

        public long ElapsedTicks
        {
            get
            {
                long delta;
                long timestamp = value;
                if (IsRunning)
                {
                    var start = timestamp;
                    var end = Stopwatch.GetTimestamp();
                    delta = end - start;
                }
                else
                {
                    delta = -timestamp;
                }

                return (long)(delta * TimestampToTicks);
            }
        }

        public static long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public static ValueStopwatch FromTimestamp(long start, long end)
        {
            return new ValueStopwatch(-(end - start));
        }

        public static ValueStopwatch StartNew()
        {
            return new ValueStopwatch(GetTimestamp());
        }

        public long GetRawTimestamp()
        {
            return value;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            // Stopwatch is stopped, therefore value is zero or negative.
            // Add the negative value to the current timestamp to start the stopwatch again.
            var timestamp = value;
            var newValue = GetTimestamp() + timestamp;
            if (newValue == 0)
            {
                newValue = 1;
            }

            value = newValue;
        }

        public void Restart()
        {
            value = GetTimestamp();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            var timestamp = value;
            var end = GetTimestamp();
            var delta = end - timestamp;
            value = -delta;
        }
    }
}