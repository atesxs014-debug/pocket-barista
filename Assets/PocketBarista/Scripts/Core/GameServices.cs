using System;
using UnityEngine;

/// <summary>
/// Tüm oyun servislerinin bir arada tutulduğu kap (GameManager oluşturur).
/// Gelir formülü burada: fiyat = taban x müşteri tipi x yükseltme x prestij x boost.
/// </summary>
public sealed class GameServices : IDisposable
{
    public PlayerEconomy Economy { get; }
    public GameStats Stats { get; }
    public LevelSystem Levels { get; }
    public UpgradeService Upgrades { get; }
    public MenuService Menu { get; }
    public StaffService Staff { get; }
    public BoostService Boost { get; }
    public PrestigeService Prestige { get; }
    public IncomeTracker Income { get; }
    public OfflineEarningsService Offline { get; }
    public QuestService Quests { get; }
    public DailyRewardService Daily { get; }
    public VenueService Venue { get; }
    public GameContentSO Content { get; }
    public ProgressionConfigSO Config { get; }

    public GameServices(SaveData data, PlayerEconomy economy, GameContentSO content, ProgressionConfigSO config, Action markDirty)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Economy = economy;

        Stats = new GameStats(data, markDirty);
        Levels = new LevelSystem(config, economy);
        Upgrades = new UpgradeService(content, data, economy, Levels, Stats);
        Menu = new MenuService(content, data, economy, Levels, Stats);
        Staff = new StaffService(content, data, economy, Levels, Stats);
        Boost = new BoostService(config, data, markDirty);
        Venue = new VenueService(content, data, economy, Levels, markDirty);
        Prestige = new PrestigeService(config, data, Stats, Venue);
        Income = new IncomeTracker(data);
        Offline = new OfflineEarningsService(config, data, economy, Upgrades, Prestige, Stats);
        Quests = new QuestService(content, data, Stats, Levels, GrantQuestReward);
        Daily = new DailyRewardService(config, data, economy, Boost, markDirty);
    }

    public void Dispose()
    {
        Quests.Dispose();
        Levels.Dispose();
    }

    /// <summary>Satış fiyatı (bahşiş hariç).</summary>
    public long PriceFor(ProductSO product, CustomerTypeSO type)
    {
        if (product == null) return 0;
        double price = product.BasePrice
                       * (type != null ? type.PriceMultiplier : 1f)
                       * Upgrades.GetMultiplier(UpgradeStat.IncomeMultiplier)
                       * Prestige.IncomeMultiplier
                       * Venue.PriceMultiplier
                       * Boost.Multiplier;
        return ToLong(price);
    }

    /// <summary>Bahşiş: müşteri tipinin şansına göre, fiyatın bir oranı.</summary>
    public long RollTip(long price, CustomerTypeSO type)
    {
        if (type == null || price <= 0 || UnityEngine.Random.value > type.TipChance) return 0;
        return ToLong(price * type.TipRatio * Upgrades.GetMultiplier(UpgradeStat.TipMultiplier));
    }

    /// <summary>Para yığını toplandığında çağrılır: para + istatistik + gelir hızı.</summary>
    public void CollectEarnings(long amount, long tipPart)
    {
        if (amount <= 0) return;
        Economy.AddMoney(amount);
        Stats.RecordEarnings(amount);
        Stats.RecordTip(tipPart);
        Income.AddEarnings(amount);
    }

    private void GrantQuestReward(QuestSO quest)
    {
        switch (quest.RewardType)
        {
            case RewardType.Money: Economy.AddMoney(quest.RewardAmount); break;
            case RewardType.XP: Economy.AddXP(quest.RewardAmount); break;
            case RewardType.IncomeBoostMinutes: Boost.AddMinutes((int)Math.Min(int.MaxValue, quest.RewardAmount)); break;
        }
    }

    private static long ToLong(double v) => v <= 0 ? 0 : v >= long.MaxValue ? long.MaxValue : (long)Math.Round(v);
}
