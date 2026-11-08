using System;

/// <summary>UTC zaman yardımcıları. Testte saati sabitlemek için Override kullanılabilir.</summary>
public static class GameClock
{
    public static Func<DateTime> Override;

    public static DateTime UtcNow => Override != null ? Override() : DateTime.UtcNow;
    public static long UtcTicks => UtcNow.Ticks;
    public static long UtcDay => UtcNow.Ticks / TimeSpan.TicksPerDay;
}
