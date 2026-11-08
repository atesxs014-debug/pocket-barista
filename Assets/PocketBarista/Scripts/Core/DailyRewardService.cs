using System;

/// <summary>
/// Günlük giriş ödülü. Art arda günlerde seri artar, bir gün kaçırılırsa 1'e döner,
/// döngü sonunda (7. gün) başa sarar ve ek boost verir.
/// </summary>
public sealed class DailyRewardService
{
    public event Action<int, long> Claimed; // (seri günü, para)

    private readonly ProgressionConfigSO _config;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly BoostService _boost;
    private readonly Action _markDirty;

    public DailyRewardService(ProgressionConfigSO config, SaveData data, PlayerEconomy economy, BoostService boost, Action markDirty)
    {
        _config = config;
        _data = data;
        _economy = economy;
        _boost = boost;
        _markDirty = markDirty;
    }

    public bool CanClaim => GameClock.UtcDay > _data.lastDailyClaimDay;

    /// <summary>Bugün alınırsa kaçıncı gün olacağı.</summary>
    public int NextStreakDay
    {
        get
        {
            bool continues = _data.lastDailyClaimDay == GameClock.UtcDay - 1;
            int day = continues ? _data.dailyStreak + 1 : 1;
            return day > _config.DailyCycleLength ? 1 : day;
        }
    }

    public long NextRewardAmount => _config.DailyReward(NextStreakDay);

    public bool TryClaim()
    {
        if (!CanClaim) return false;

        int day = NextStreakDay;
        long amount = _config.DailyReward(day);
        _data.dailyStreak = day;
        _data.lastDailyClaimDay = GameClock.UtcDay;

        _economy.AddMoney(amount);
        if (day == _config.DailyCycleLength) _boost.AddMinutes(_config.DailyBoostMinutesOnLastDay);

        _markDirty?.Invoke();
        UnityEngine.Debug.Log($"[Daily] Gün {day}: +{amount}");
        Claimed?.Invoke(day, amount);
        return true;
    }
}
