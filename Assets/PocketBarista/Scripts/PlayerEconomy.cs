using System;
using UnityEngine;

/// <summary>
/// Saf C# ekonomi mantığı. MonoBehaviour değildir; UI, kayıt ve sahneden bağımsızdır,
/// bu yüzden Edit Mode testleriyle doğrudan test edilebilir.
/// Para ve XP 'long' tutulur: tycoon oyunlarında int (≈2.1 milyar) taşabilir.
/// </summary>
public sealed class PlayerEconomy
{
    /// <summary>Yeni bakiye ile tetiklenir. Sadece değer gerçekten değiştiğinde.</summary>
    public event Action<long> MoneyChanged;

    /// <summary>Yeni XP ile tetiklenir.</summary>
    public event Action<long> XPChanged;

    /// <summary>Harcama başarısız olduğunda (istenen, mevcut). UI "yetersiz bakiye" animasyonu için.</summary>
    public event Action<long, long> InsufficientFunds;

    public long Money { get; private set; }
    public long XP { get; private set; }

    /// <summary>Son kayıttan beri değişiklik var mı?</summary>
    public bool IsDirty { get; private set; }

    public PlayerEconomy(long startingMoney, long startingXP)
    {
        // Bozuk/negatif kayıt verisine karşı koruma
        Money = startingMoney < 0 ? 0 : startingMoney;
        XP = startingXP < 0 ? 0 : startingXP;
        IsDirty = false;
    }

    public bool CanAfford(long amount) => amount >= 0 && Money >= amount;

    public bool AddMoney(long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Economy] AddMoney geçersiz miktar: {amount}");
            return false;
        }

        long previous = Money;
        Money = SaturatingAdd(Money, amount);
        if (Money == previous) return false; // Tavana ulaşılmış

        IsDirty = true;
        MoneyChanged?.Invoke(Money);
        return true;
    }

    public bool TrySpendMoney(long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Economy] TrySpendMoney geçersiz miktar: {amount}");
            return false;
        }

        if (Money < amount)
        {
            InsufficientFunds?.Invoke(amount, Money);
            return false;
        }

        Money -= amount;
        IsDirty = true;
        MoneyChanged?.Invoke(Money);
        return true;
    }

    public bool AddXP(long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Economy] AddXP geçersiz miktar: {amount}");
            return false;
        }

        long previous = XP;
        XP = SaturatingAdd(XP, amount);
        if (XP == previous) return false;

        IsDirty = true;
        XPChanged?.Invoke(XP);
        return true;
    }

    /// <summary>Durumu verilen buffer'a yazar (yeni nesne üretmez).</summary>
    public void WriteTo(SaveData target)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        target.money = Money;
        target.xp = XP;
    }

    public void ClearDirty() => IsDirty = false;

    // Taşma olursa long.MaxValue'da durur, negatife dönmez.
    private static long SaturatingAdd(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;
}
