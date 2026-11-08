using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yükseltme seviyeleri ve stat çarpanları. Çarpanlar bir dizide önbelleğe alınır;
/// GetMultiplier her karede çağrılabilir (allocation yok).
/// </summary>
public sealed class UpgradeService
{
    public event Action<UpgradeSO, int> UpgradeChanged;

    private readonly GameContentSO _content;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly LevelSystem _levels;
    private readonly GameStats _stats;
    private readonly Dictionary<UpgradeSO, int> _levelsByUpgrade = new Dictionary<UpgradeSO, int>();
    private readonly float[] _statValues;

    public UpgradeService(GameContentSO content, SaveData data, PlayerEconomy economy, LevelSystem levels, GameStats stats)
    {
        _content = content;
        _data = data;
        _economy = economy;
        _levels = levels;
        _stats = stats;
        _statValues = new float[Enum.GetValues(typeof(UpgradeStat)).Length];

        foreach (IdLevel saved in data.upgrades)
        {
            UpgradeSO up = content.FindUpgrade(saved.id);
            if (up == null) { Debug.LogWarning($"[Upgrade] Bilinmeyen yükseltme kaydı: {saved.id}"); continue; }
            _levelsByUpgrade[up] = Mathf.Clamp(saved.level, 0, up.MaxLevel);
        }
        RebuildCache();
    }

    public int GetLevel(UpgradeSO upgrade) => upgrade != null && _levelsByUpgrade.TryGetValue(upgrade, out int l) ? l : 0;
    public bool IsMaxed(UpgradeSO upgrade) => GetLevel(upgrade) >= upgrade.MaxLevel;
    public bool IsUnlocked(UpgradeSO upgrade) => upgrade != null && _levels.Level >= upgrade.UnlockLevel;
    public long GetNextCost(UpgradeSO upgrade) => upgrade.GetCost(GetLevel(upgrade));

    public bool CanBuy(UpgradeSO upgrade) =>
        upgrade != null && IsUnlocked(upgrade) && !IsMaxed(upgrade) && _economy.CanAfford(GetNextCost(upgrade));

    public bool TryBuy(UpgradeSO upgrade)
    {
        if (upgrade == null || !IsUnlocked(upgrade) || IsMaxed(upgrade)) return false;
        if (!_economy.TrySpendMoney(GetNextCost(upgrade))) return false;

        int level = GetLevel(upgrade) + 1;
        _levelsByUpgrade[upgrade] = level;
        WriteToSave(upgrade, level);
        RebuildCache();
        _stats.RecordUpgradeBought();

        Debug.Log($"[Upgrade] {upgrade.Id} → {level}");
        UpgradeChanged?.Invoke(upgrade, level);
        return true;
    }

    /// <summary>Çarpan statları için 1 + toplam; OfflineCapHours için eklenen saat.</summary>
    public float GetMultiplier(UpgradeStat stat)
    {
        float v = _statValues[(int)stat];
        return stat == UpgradeStat.OfflineCapHours ? v : 1f + v;
    }

    public void ResetAll()
    {
        _levelsByUpgrade.Clear();
        _data.upgrades.Clear();
        RebuildCache();
    }

    private void WriteToSave(UpgradeSO upgrade, int level)
    {
        for (int i = 0; i < _data.upgrades.Count; i++)
        {
            if (_data.upgrades[i].id == upgrade.Id) { _data.upgrades[i].level = level; return; }
        }
        _data.upgrades.Add(new IdLevel { id = upgrade.Id, level = level });
    }

    private void RebuildCache()
    {
        Array.Clear(_statValues, 0, _statValues.Length);
        foreach (var pair in _levelsByUpgrade)
            _statValues[(int)pair.Key.Stat] += pair.Key.GetValue(pair.Value);
    }
}
