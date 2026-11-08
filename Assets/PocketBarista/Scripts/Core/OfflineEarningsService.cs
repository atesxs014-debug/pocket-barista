using System;

/// <summary>
/// Açılışta, son kayıttan beri geçen süreye göre çevrimdışı kazancı hesaplar.
/// Popup Pending > 0 ise gösterilir; Claim() parayı ekler.
/// </summary>
public sealed class OfflineEarningsService
{
    public event Action<long, double> EarningsReady; // (miktar, geçen saniye)

    private readonly ProgressionConfigSO _config;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly UpgradeService _upgrades;
    private readonly PrestigeService _prestige;
    private readonly GameStats _stats;

    public long Pending { get; private set; }
    public double AwaySeconds { get; private set; }

    public OfflineEarningsService(ProgressionConfigSO config, SaveData data, PlayerEconomy economy,
        UpgradeService upgrades, PrestigeService prestige, GameStats stats)
    {
        _config = config;
        _data = data;
        _economy = economy;
        _upgrades = upgrades;
        _prestige = prestige;
        _stats = stats;
    }

    public double CapHours => _config.BaseOfflineCapHours + _upgrades.GetMultiplier(UpgradeStat.OfflineCapHours);

    /// <summary>Yükleme sonrası bir kez çağrılır.</summary>
    public void Evaluate()
    {
        Pending = 0;
        if (_data.lastSaveUtcTicks <= 0) return; // İlk açılış

        long elapsedTicks = GameClock.UtcTicks - _data.lastSaveUtcTicks;
        if (elapsedTicks <= 0) return; // Saat geri alınmış: ödül yok

        AwaySeconds = elapsedTicks / (double)TimeSpan.TicksPerSecond;
        if (AwaySeconds < _config.MinOfflineMinutes * 60.0) return;

        double effectiveSeconds = Math.Min(AwaySeconds, CapHours * 3600.0);
        double rate = _config.BaseOfflineRate * _upgrades.GetMultiplier(UpgradeStat.OfflineEarningRate);
        double amount = _data.incomePerSecond * effectiveSeconds * rate;

        Pending = amount >= long.MaxValue ? long.MaxValue : (long)amount;
        if (Pending > 0)
        {
            UnityEngine.Debug.Log($"[Offline] {AwaySeconds / 60.0:F0} dk yokluk → {Pending}");
            EarningsReady?.Invoke(Pending, AwaySeconds);
        }
    }

    /// <summary>multiplier: ileride "2x al" gibi seçenekler için.</summary>
    public long Claim(float multiplier = 1f)
    {
        if (Pending <= 0) return 0;
        double total = Pending * Math.Max(1f, multiplier);
        long amount = total >= long.MaxValue ? long.MaxValue : (long)total;
        Pending = 0;
        _economy.AddMoney(amount);
        _stats.RecordEarnings(amount);
        return amount;
    }
}
