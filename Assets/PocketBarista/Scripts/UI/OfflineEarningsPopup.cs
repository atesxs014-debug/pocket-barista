using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>"Sen yokken $1.2K kazandın" popup'ı. GameManager.Start'ta Offline.Evaluate tetikler.</summary>
public sealed class OfflineEarningsPopup : UIPanel
{
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private TMP_Text awayLabel;
    [SerializeField] private Button claimButton;

    private readonly char[] _amountBuf = new char[32];
    private readonly char[] _awayBuf = new char[32];
    private GameServices _services;

    protected override void Awake()
    {
        base.Awake();
        // GameManager -1000 sırasıyla önce Awake olur; Evaluate GameManager.Start'ta, bu Awake'ten sonra çalışır
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) return;
        _services.Offline.EarningsReady += HandleReady;
        if (claimButton != null) claimButton.onClick.AddListener(Claim);
    }

    private void OnDestroy()
    {
        if (_services != null) _services.Offline.EarningsReady -= HandleReady;
    }

    private void HandleReady(long amount, double awaySeconds)
    {
        UIText.SetNumber(amountLabel, _amountBuf, amount, "$");
        UIText.SetDuration(awayLabel, _awayBuf, awaySeconds);
        Open();
    }

    private void Claim()
    {
        long got = _services.Offline.Claim();
        if (got > 0) AudioManager.Play(SfxType.Coin);
        Close();
    }
}
