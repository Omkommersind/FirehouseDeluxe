using System;

namespace FirehouseDeluxe
{
    internal static class RunTimeFormatter
    {
        public static string Format(double elapsedSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(Math.Floor(Math.Max(0d, elapsedSeconds)));
            return duration.TotalHours >= 1d
                ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes:00}:{duration.Seconds:00}";
        }
    }
}
