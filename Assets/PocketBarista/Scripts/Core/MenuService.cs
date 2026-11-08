using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Menüde açık ürünler ve rastgele sipariş seçimi.</summary>
public sealed class MenuService
{
    public event Action<ProductSO> ProductUnlocked;

    private readonly GameContentSO _content;
    private readonly SaveData _data;
    private readonly PlayerEconomy _economy;
    private readonly LevelSystem _levels;
    private readonly GameStats _stats;
    private readonly HashSet<ProductSO> _unlocked = new HashSet<ProductSO>();
    private readonly List<ProductSO> _candidates = new List<ProductSO>(32); // Tekrar kullanılır

    public MenuService(GameContentSO content, SaveData data, PlayerEconomy economy, LevelSystem levels, GameStats stats)
    {
        _content = content;
        _data = data;
        _economy = economy;
        _levels = levels;
        _stats = stats;
        LoadFromSave();
    }

    public bool IsUnlocked(ProductSO product) => product != null && _unlocked.Contains(product);
    public bool IsAvailableToUnlock(ProductSO product) => product != null && !IsUnlocked(product) && _levels.Level >= product.UnlockLevel;
    public bool CanUnlock(ProductSO product) => IsAvailableToUnlock(product) && _economy.CanAfford(product.UnlockCost);

    public bool TryUnlock(ProductSO product)
    {
        if (!IsAvailableToUnlock(product)) return false;
        if (product.UnlockCost > 0 && !_economy.TrySpendMoney(product.UnlockCost)) return false;

        _unlocked.Add(product);
        _data.unlockedProducts.Add(product.Id);
        _stats.RecordProductUnlocked();
        Debug.Log($"[Menu] Açıldı: {product.Id}");
        ProductUnlocked?.Invoke(product);
        return true;
    }

    /// <summary>Sahnede istasyonu olan açık ürünlerden rastgele birini seçer. Yoksa null.</summary>
    public ProductSO PickRandomOrder()
    {
        _candidates.Clear();
        foreach (ProductSO p in _unlocked)
        {
            if (StationRegistry.HasStation(p.Station)) _candidates.Add(p);
        }
        return _candidates.Count == 0 ? null : _candidates[UnityEngine.Random.Range(0, _candidates.Count)];
    }

    public void ResetToDefaults()
    {
        _data.unlockedProducts.Clear();
        _unlocked.Clear();
        LoadFromSave();
    }

    private void LoadFromSave()
    {
        foreach (string id in _data.unlockedProducts)
        {
            ProductSO p = _content.FindProduct(id);
            if (p != null) _unlocked.Add(p);
        }
        foreach (ProductSO p in _content.Products)
        {
            if (p != null && p.UnlockedByDefault && _unlocked.Add(p)) _data.unlockedProducts.Add(p.Id);
        }
    }
}
