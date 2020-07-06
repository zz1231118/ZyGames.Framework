using System;

namespace ZyGames.Framework.Linq
{
    internal static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double value)
        {
            double doubleTicks = checked(timeSpan.Ticks * value);
            long ticks = checked((long)doubleTicks);
            return TimeSpan.FromTicks(ticks);
        }
    }
}
