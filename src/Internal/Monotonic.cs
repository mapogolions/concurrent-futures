using System.Diagnostics;

namespace Future.Internal
{
    // High resolution ticks
    internal static class Monotonic
    {
        public static long Ticks(TimeSpan t)
        {
            return (long)(t.TotalSeconds * Stopwatch.Frequency);
        }
        
        public static TimeSpan FromTicks(long ticks)
        {
            return TimeSpan.FromTicks((long)((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency * ticks));
        }
    }
}
