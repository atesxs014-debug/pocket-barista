using System;

/// <summary>
/// Toplam XP'den seviye hesaplar. XP PlayerEconomy'de tutulur (prestij puanı / "XP").
/// Seviye atlayınca yeni ürün, eşya, müşteri tipi ve yükseltmeler açılır.
/// </summary>
public sealed class LevelSystem : IDisposable
{
    public event Action<int> LevelChanged;

    public int Level { get; private set; } = 1;
    public long XpIntoLevel { get; private set; }
    public long XpForNext { get; private set; }
    public float Progress01 => XpForNext <= 0 ? 1f : (float)XpIntoLevel / XpForNext;

    private readonly ProgressionConfigSO _config;
    private readonly PlayerEconomy _economy;

    public LevelSystem(ProgressionConfigSO config, PlayerEconomy economy)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        Recalculate(_economy.XP, raiseEvents: false);
        _economy.XPChanged += HandleXPChanged;
    }

    public void Dispose() => _economy.XPChanged -= HandleXPChanged;

    private void HandleXPChanged(long totalXp) => Recalculate(totalXp, raiseEvents: true);

    private void Recalculate(long totalXp, bool raiseEvents)
    {
        int previous = Level;
        int level = 1;
        long remaining = totalXp;
        long need = _config.XpForNextLevel(level);

        while (level < _config.MaxLevel && remaining >= need)
        {
            remaining -= need;
            level++;
            need = _config.XpForNextLevel(level);
        }

        Level = level;
        XpIntoLevel = remaining;
        XpForNext = level >= _config.MaxLevel ? 0 : need;

        if (raiseEvents && level != previous)
        {
            UnityEngine.Debug.Log($"[Level] {previous} → {level}");
            LevelChanged?.Invoke(level);
        }
    }
}
