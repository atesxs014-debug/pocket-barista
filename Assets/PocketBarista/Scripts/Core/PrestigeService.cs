using System;

/// <summary>
/// Yeniden doğuş = sonraki mekana taşınma: dükkan, para, yükseltmeler, menü ve personel sıfırlanır; kazanılan yıldızlar kalıcı gelir çarpanı verir.
/// Sıfırlamayı GameManager.Prestige() yapar (sahneyi yeniden yükler).
/// </summary>
public sealed class PrestigeService
{
    private readonly ProgressionConfigSO _config;
    private readonly SaveData _data;
    private readonly GameStats _stats;
    private readonly VenueService _venue;

    public PrestigeService(ProgressionConfigSO config, SaveData data, GameStats stats, VenueService venue)
    {
        _config = config;
        _data = data;
        _stats = stats;
        _venue = venue;
    }

    public long Stars => _data.stars;
    public int PrestigeCount => _data.prestigeCount;
    public float IncomeMultiplier => 1f + _data.stars * _config.BonusPerStar;

    /// <summary>Şimdi yeniden doğarsan kazanacağın yeni yıldız.</summary>
    public long PendingStars => Math.Max(0, _config.StarsFor(_stats.RunEarnings / Math.Max(1e-6, _venue.PrestigeThresholdMultiplier)));
    public bool CanPrestige => PendingStars > 0;

    /// <summary>Yıldızları ekler. Geri kalan sıfırlamayı çağıran yapar.</summary>
    public long CommitStars()
    {
        long gained = PendingStars;
        if (gained <= 0) return 0;
        _data.stars += gained;
        _data.prestigeCount++;
        return gained;
    }
}
