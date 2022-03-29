using System;

namespace ZyGames.Framework.Linq
{
    internal static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double value)
        {
            var doubleTicks = checked(timeSpan.Ticks * value);
            var ticks = checked((long)doubleTicks);
            return TimeSpan.FromTicks(ticks);
        }
    }
}
