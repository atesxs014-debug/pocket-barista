using System;
using System.Collections.Generic;

/// <summary>
/// Mevcut mekan ve mekan içindeki açılmış bölgeler. Mekan değişimi yeniden doğuşla olur (GameManager.Prestige).
/// </summary>
public sealed class VenueService
{
    public event Action<string> AreaUnlocked;

    private readonly GameContentSO _content;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly LevelSystem _levels;
    private readonly Action _markDirty;
    private readonly HashSet<string> _areas = new HashSet<string>(StringComparer.Ordinal);

    public VenueService(GameContentSO content, SaveData data, PlayerEconomy economy, LevelSystem levels, Action markDirty)
    {
        _content = content;
        _data = data;
        _economy = economy;
        _levels = levels;
        _markDirty = markDirty;
        if (_content.Venues.Count > 0 && _data.venueIndex >= _content.Venues.Count) _data.venueIndex = _content.Venues.Count - 1;
        for (int i = 0; i < _data.unlockedAreas.Count; i++) _areas.Add(_data.unlockedAreas[i]);
    }

    public int Index => _data.venueIndex;
    public int Count => _content.Venues.Count;
    public VenueSO Current => Count > 0 ? _content.Venues[_data.venueIndex] : null;
    public bool IsLastVenue => _data.venueIndex >= Count - 1;
    public VenueSO Next => IsLastVenue ? null : _content.Venues[_data.venueIndex + 1];
    public float PriceMultiplier => Current != null ? Current.PriceMultiplier : 1f;
    public double PrestigeThresholdMultiplier => Current != null ? Current.PrestigeThresholdMultiplier : 1.0;

    public bool IsAreaUnlocked(string areaId) => !string.IsNullOrEmpty(areaId) && _areas.Contains(areaId);

    public bool CanUnlockArea(string areaId, long cost, int requiredLevel) =>
        !IsAreaUnlocked(areaId) && _levels.Level >= requiredLevel && _economy.CanAfford(cost);

    public bool TryUnlockArea(string areaId, long cost, int requiredLevel)
    {
        if (string.IsNullOrEmpty(areaId) || IsAreaUnlocked(areaId)) return false;
        if (_levels.Level < requiredLevel) return false;
        if (!_economy.TrySpendMoney(cost)) return false;

        _areas.Add(areaId);
        _data.unlockedAreas.Add(areaId);
        _markDirty?.Invoke();
        UnityEngine.Debug.Log($"[Venue] Bölge açıldı: {areaId} ({Current?.Id})");
        AreaUnlocked?.Invoke(areaId);
        return true;
    }

    /// <summary>Yeniden doğuşta çağrılır: sonraki mekana geçer (sondaysa kalır), bölgeleri sıfırlar.</summary>
    public void AdvanceForPrestige()
    {
        if (!IsLastVenue) _data.venueIndex++;
        _areas.Clear();
        _data.unlockedAreas.Clear();
    }
}
