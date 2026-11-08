using System;
using UnityEngine;

/// <summary>
/// İlerleme dengesi: seviye eğrisi, prestij, çevrimdışı kazanç, günlük ödül.
/// Create > Tycoon > Progression Config. Tek bir asset GameManager'a atanır.
/// </summary>
[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Tycoon/Progression Config")]
public sealed class ProgressionConfigSO : ScriptableObject
{
    [Header("Seviye (XP)")]
    [SerializeField, Tooltip("Seviye 1→2 için gereken XP")] private long baseXpPerLevel = 50;
    [SerializeField, Tooltip("Her seviyede gereken XP çarpanı")] private double xpGrowth = 1.35;
    [SerializeField] private int maxLevel = 100;

    [Header("Prestij (yeniden doğuş)")]
    [SerializeField, Tooltip("İlk yıldız için gereken toplam kazanç (bu turda)")] private double prestigeThreshold = 1_000_000;
    [SerializeField, Tooltip("Her yıldızın kalıcı gelir bonusu (0.1 = +%10)")] private float bonusPerStar = 0.1f;

    [Header("Çevrimdışı kazanç")]
    [SerializeField] private float baseOfflineCapHours = 2f;
    [SerializeField, Tooltip("Oyun içi gelir hızının yüzde kaçı")] private float baseOfflineRate = 0.25f;
    [SerializeField, Tooltip("Bundan kısa yokluk popup göstermez (dakika)")] private float minOfflineMinutes = 2f;

    [Header("Günlük ödül (seri günü → para)")]
    [SerializeField] private long[] dailyRewards = { 100, 200, 400, 700, 1000, 1500, 3000 };
    [SerializeField, Tooltip("7. gün ek olarak verilen boost dakikası")] private int dailyBoostMinutesOnLastDay = 30;

    [Header("Boost")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField, Tooltip("Birikmiş boost en fazla kaç dakika olabilir")] private int maxBoostMinutes = 240;

    public int MaxLevel => maxLevel;
    public float BonusPerStar => bonusPerStar;
    public float BaseOfflineCapHours => baseOfflineCapHours;
    public float BaseOfflineRate => baseOfflineRate;
    public float MinOfflineMinutes => minOfflineMinutes;
    public int DailyBoostMinutesOnLastDay => dailyBoostMinutesOnLastDay;
    public float BoostMultiplier => boostMultiplier;
    public int MaxBoostMinutes => maxBoostMinutes;
    public int DailyCycleLength => dailyRewards.Length;

    /// <summary>level → level+1 için gereken XP.</summary>
    public long XpForNextLevel(int level)
    {
        double xp = baseXpPerLevel * Math.Pow(xpGrowth, Math.Max(0, level - 1));
        return xp >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(xp);
    }

    /// <summary>Bu turdaki toplam kazançla elde edilecek toplam yıldız (karekök eğrisi).</summary>
    public long StarsFor(double runEarnings)
    {
        if (runEarnings < prestigeThreshold || prestigeThreshold <= 0) return 0;
        return (long)Math.Floor(Math.Sqrt(runEarnings / prestigeThreshold));
    }

    public long DailyReward(int streakDay)
    {
        if (dailyRewards == null || dailyRewards.Length == 0) return 0;
        int i = Mathf.Clamp(streakDay, 1, dailyRewards.Length) - 1;
        return dailyRewards[i];
    }

    private void OnValidate()
    {
        if (baseXpPerLevel < 1) baseXpPerLevel = 1;
        if (xpGrowth < 1.0) xpGrowth = 1.0;
        if (maxLevel < 2) maxLevel = 2;
        if (prestigeThreshold < 1) prestigeThreshold = 1;
        bonusPerStar = Mathf.Max(0f, bonusPerStar);
        baseOfflineCapHours = Mathf.Max(0f, baseOfflineCapHours);
        baseOfflineRate = Mathf.Clamp01(baseOfflineRate);
        boostMultiplier = Mathf.Max(1f, boostMultiplier);
        if (dailyRewards == null || dailyRewards.Length == 0) dailyRewards = new long[] { 100 };
    }
}
