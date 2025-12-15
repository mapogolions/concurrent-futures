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
        
        public static TimeSpan FromTicks(long monoTicks)
        {
            return System.TimeSpan.FromTicks((long)((double)System.TimeSpan.TicksPerSecond / Stopwatch.Frequency * monoTicks));
        }
    }
}
