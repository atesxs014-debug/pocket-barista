using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Personel işe alma (bir kerelik ücret, maaş yok). Sahnedeki StaffSpawner bu event'leri dinler.</summary>
public sealed class StaffService
{
    public event Action<StaffSO> StaffHired;

    private readonly GameContentSO _content;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly LevelSystem _levels;
    private readonly GameStats _stats;
    private readonly List<StaffSO> _hired = new List<StaffSO>();

    public IReadOnlyList<StaffSO> Hired => _hired;

    public StaffService(GameContentSO content, SaveData data, PlayerEconomy economy, LevelSystem levels, GameStats stats)
    {
        _content = content;
        _data = data;
        _economy = economy;
        _levels = levels;
        _stats = stats;
        foreach (string id in data.hiredStaff)
        {
            StaffSO s = content.FindStaff(id);
            if (s != null) _hired.Add(s);
            else Debug.LogWarning($"[Staff] Bilinmeyen personel kaydı: {id}");
        }
    }

    public bool IsHired(StaffSO staff) => _hired.Contains(staff);
    public bool IsUnlocked(StaffSO staff) => staff != null && _levels.Level >= staff.UnlockLevel;
    public bool CanHire(StaffSO staff) => staff != null && !IsHired(staff) && IsUnlocked(staff) && _economy.CanAfford(staff.HireCost);

    public bool TryHire(StaffSO staff)
    {
        if (staff == null || IsHired(staff) || !IsUnlocked(staff)) return false;
        if (!_economy.TrySpendMoney(staff.HireCost)) return false;

        _hired.Add(staff);
        _data.hiredStaff.Add(staff.Id);
        _stats.RecordStaffHired();
        Debug.Log($"[Staff] İşe alındı: {staff.Id} ({staff.Role})");
        StaffHired?.Invoke(staff);
        return true;
    }

    public void ResetAll()
    {
        _hired.Clear();
        _data.hiredStaff.Clear();
    }
}
