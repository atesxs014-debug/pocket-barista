using System;
using UnityEngine;

/// <summary>
/// Seviyeli yükseltme. Fiyat üstel artar: cost(n) = baseCost * costGrowth^n.
/// Değer toplamsal artar: value(n) = valuePerLevel * n (çarpan statlarında 1 + value).
/// Create > Tycoon > Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Tycoon/Upgrade")]
public sealed class UpgradeSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string nameKey;
    [SerializeField] private Sprite icon;
    [SerializeField] private UpgradeStat stat = UpgradeStat.IncomeMultiplier;
    [SerializeField] private int maxLevel = 25;
    [SerializeField] private long baseCost = 100;
    [SerializeField, Tooltip("Her seviyede fiyat çarpanı (1.15 tipik)")] private double costGrowth = 1.15;
    [SerializeField, Tooltip("Seviye başına eklenen değer (0.1 = +%10)")] private float valuePerLevel = 0.1f;
    [SerializeField] private int unlockLevel = 1;

    public string Id => id;
    public string NameKey => nameKey;
    public Sprite Icon => icon;
    public UpgradeStat Stat => stat;
    public int MaxLevel => maxLevel;
    public int UnlockLevel => unlockLevel;
    public float ValuePerLevel => valuePerLevel;

    /// <summary>currentLevel'den bir sonrakine geçiş fiyatı. Taşmada long.MaxValue.</summary>
    public long GetCost(int currentLevel)
    {
        if (currentLevel >= maxLevel) return long.MaxValue;
        double cost = baseCost * Math.Pow(costGrowth, currentLevel);
        return cost >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(cost);
    }

    public float GetValue(int level) => valuePerLevel * Mathf.Clamp(level, 0, maxLevel);

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(nameKey)) nameKey = "upgrade." + id;
        if (maxLevel < 1) maxLevel = 1;
        if (baseCost < 1) baseCost = 1;
        if (costGrowth < 1.0) costGrowth = 1.0;
        if (unlockLevel < 1) unlockLevel = 1;
    }
}
