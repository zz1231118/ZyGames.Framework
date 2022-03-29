using System;

namespace ZyGames.Framework.Services.Dashboard
{
    internal static class Extensions
    {
        public static string ToPeriodString(this DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}
