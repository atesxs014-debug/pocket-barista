using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Günlük giriş ödülü: 7 günlük seri, son gün boost. Alınabiliyorsa açılışta kendiliğinden açılır.</summary>
public sealed class DailyRewardPopup : UIPanel
{
    [SerializeField] private TMP_Text dayLabel;
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject[] dayMarkers = new GameObject[0]; // Seri göstergesi (opsiyonel)
    [SerializeField] private GameObject hudBadge;                         // HUD'daki "!" rozeti
    [SerializeField] private bool autoOpenOnStart = true;

    private readonly char[] _dayBuf = new char[16];
    private readonly char[] _amountBuf = new char[32];
    private GameServices _services;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        if (claimButton != null) claimButton.onClick.AddListener(Claim);
        RefreshBadge();
        if (autoOpenOnStart && _services.Daily.CanClaim) Open();
    }

    protected override void OnOpened()
    {
        if (_services == null) return;
        int day = _services.Daily.NextStreakDay;
        UIText.SetNumber(dayLabel, _dayBuf, day);
        UIText.SetNumber(amountLabel, _amountBuf, _services.Daily.NextRewardAmount, "$");
        for (int i = 0; i < dayMarkers.Length; i++) if (dayMarkers[i] != null) dayMarkers[i].SetActive(i < day);
        if (claimButton != null) claimButton.interactable = _services.Daily.CanClaim;
    }

    private void Claim()
    {
        long amount = _services.Daily.NextRewardAmount;
        if (_services.Daily.TryClaim())
        {
            AudioManager.Play(SfxType.Coin);
            Toast.Show(ToastKind.DailyClaimed, amount);
        }
        RefreshBadge();
        Close();
    }

    private void RefreshBadge() { if (hudBadge != null) hudBadge.SetActive(_services.Daily.CanClaim); }
}
