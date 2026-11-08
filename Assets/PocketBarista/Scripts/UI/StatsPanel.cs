using TMPro;
using UnityEngine;

/// <summary>İstatistik ekranı: toplam kazanç, servis edilen / kaçan müşteri, bahşiş, prestij sayısı.</summary>
public sealed class StatsPanel : UIPanel
{
    [SerializeField] private TMP_Text lifetimeEarnings;
    [SerializeField] private TMP_Text runEarnings;
    [SerializeField] private TMP_Text served;
    [SerializeField] private TMP_Text lost;
    [SerializeField] private TMP_Text tips;
    [SerializeField] private TMP_Text prestigeCount;

    private readonly char[] _b1 = new char[32], _b2 = new char[32], _b3 = new char[32],
                            _b4 = new char[32], _b5 = new char[32], _b6 = new char[32];

    protected override void OnOpened()
    {
        GameServices s = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (s == null) return;
        UIText.SetNumber(lifetimeEarnings, _b1, s.Stats.LifetimeEarnings, "$");
        UIText.SetNumber(runEarnings, _b2, s.Stats.RunEarnings, "$");
        UIText.SetNumber(served, _b3, s.Stats.CustomersServed);
        UIText.SetNumber(lost, _b4, s.Stats.CustomersLost);
        UIText.SetNumber(tips, _b5, s.Stats.TipsCollected, "$");
        UIText.SetNumber(prestigeCount, _b6, s.Prestige.PrestigeCount);
    }
}
