using System;

/// <summary>Süreli gelir boost'u (görev ve günlük ödülden). Bitiş zamanı UTC olarak kayıtlıdır, oyun kapalıyken de akar.</summary>
public sealed class BoostService
{
    public event Action BoostChanged;

    private readonly ProgressionConfigSO _config;
    private readonly SaveData _data;
    private readonly Action _markDirty;

    public BoostService(ProgressionConfigSO config, SaveData data, Action markDirty)
    {
        _config = config;
        _data = data;
        _markDirty = markDirty;
    }

    public bool IsActive => _data.boostEndUtcTicks > GameClock.UtcTicks;
    public float Multiplier => IsActive ? _config.BoostMultiplier : 1f;

    public double RemainingSeconds
    {
        get
        {
            long remaining = _data.boostEndUtcTicks - GameClock.UtcTicks;
            return remaining > 0 ? remaining / (double)TimeSpan.TicksPerSecond : 0;
        }
    }

    public void AddMinutes(int minutes)
    {
        if (minutes <= 0) return;
        long now = GameClock.UtcTicks;
        long start = Math.Max(now, _data.boostEndUtcTicks);
        long end = start + minutes * TimeSpan.TicksPerMinute;
        long cap = now + _config.MaxBoostMinutes * TimeSpan.TicksPerMinute;
        _data.boostEndUtcTicks = Math.Min(end, cap);
        _markDirty?.Invoke();
        BoostChanged?.Invoke();
    }
}
