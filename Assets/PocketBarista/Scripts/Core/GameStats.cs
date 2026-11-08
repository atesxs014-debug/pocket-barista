using System;

/// <summary>
/// Oyun istatistikleri (görevler, prestij ve çevrimdışı kazanç bunları kullanır).
/// Veri SaveData'da tutulur; bu sınıf değişiklikleri event ile duyurur.
/// </summary>
public sealed class GameStats
{
    /// <summary>Herhangi bir istatistik değişince (görev ilerlemesi için).</summary>
    public event Action<QuestType> Changed;
    public event Action CustomerLost;

    private readonly SaveData _data;
    private readonly Action _markDirty;

    public GameStats(SaveData data, Action markDirty)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _markDirty = markDirty;
    }

    public double RunEarnings => _data.runEarnings;
    public double LifetimeEarnings => _data.lifetimeEarnings;
    public long CustomersServed => _data.customersServed;
    public long CustomersLost => _data.customersLost;
    public long TipsCollected => _data.tipsCollected;
    public long ItemsPlaced => _data.itemsPlaced;
    public long UpgradesBought => _data.upgradesBought;
    public long StaffHired => _data.staffHired;
    public long ProductsUnlocked => _data.productsUnlocked;

    public void RecordEarnings(long amount)
    {
        if (amount <= 0) return;
        _data.runEarnings += amount;
        _data.lifetimeEarnings += amount;
        Raise(QuestType.EarnMoney);
    }

    public void RecordTip(long amount)
    {
        if (amount <= 0) return;
        _data.tipsCollected += amount;
        Raise(QuestType.CollectTips);
    }

    public void RecordCustomerServed() { _data.customersServed++; Raise(QuestType.ServeCustomers); }
    public void RecordItemPlaced() { _data.itemsPlaced++; Raise(QuestType.PlaceItems); }
    public void RecordUpgradeBought() { _data.upgradesBought++; Raise(QuestType.BuyUpgrades); }
    public void RecordStaffHired() { _data.staffHired++; Raise(QuestType.HireStaff); }
    public void RecordProductUnlocked() { _data.productsUnlocked++; Raise(QuestType.UnlockProducts); }

    public void RecordCustomerLost()
    {
        _data.customersLost++;
        _markDirty?.Invoke();
        CustomerLost?.Invoke();
    }

    /// <summary>Görev sistemi için: tipe göre güncel değer.</summary>
    public long GetValue(QuestType type)
    {
        switch (type)
        {
            case QuestType.EarnMoney: return _data.runEarnings >= long.MaxValue ? long.MaxValue : (long)_data.runEarnings;
            case QuestType.ServeCustomers: return _data.customersServed;
            case QuestType.PlaceItems: return _data.itemsPlaced;
            case QuestType.BuyUpgrades: return _data.upgradesBought;
            case QuestType.HireStaff: return _data.staffHired;
            case QuestType.UnlockProducts: return _data.productsUnlocked;
            case QuestType.CollectTips: return _data.tipsCollected;
            default: return 0;
        }
    }

    /// <summary>Prestij: tur istatistiklerini sıfırlar, tüm zamanlar korunur.</summary>
    public void ResetRun()
    {
        _data.runEarnings = 0;
        _markDirty?.Invoke();
    }

    private void Raise(QuestType type)
    {
        _markDirty?.Invoke();
        Changed?.Invoke(type);
    }
}
