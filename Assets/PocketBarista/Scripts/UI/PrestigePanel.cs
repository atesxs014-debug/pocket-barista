using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yeniden doğuş (prestij) ekranı: mevcut yıldız, kazanılacak yıldız, yeni gelir çarpanı.
/// Onay iki adımlı: ilk dokunuş "Emin misin?" gösterir.
/// </summary>
public sealed class PrestigePanel : UIPanel
{
    [SerializeField] private TMP_Text starsLabel;
    [SerializeField] private TMP_Text pendingLabel;
    [SerializeField] private TMP_Text multiplierLabel;
    [SerializeField] private Button prestigeButton;
    [SerializeField] private GameObject confirmGroup;
    [SerializeField] private Button confirmButton;

    private readonly char[] _starsBuf = new char[32];
    private readonly char[] _pendingBuf = new char[32];
    private readonly char[] _multBuf = new char[32];
    private GameServices _services;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        if (prestigeButton != null) prestigeButton.onClick.AddListener(AskConfirm);
        if (confirmButton != null) confirmButton.onClick.AddListener(DoPrestige);
    }

    protected override void OnOpened()
    {
        if (_services == null) return;
        PrestigeService p = _services.Prestige;
        long pending = p.PendingStars;
        UIText.SetNumber(starsLabel, _starsBuf, p.Stars);
        UIText.SetNumber(pendingLabel, _pendingBuf, pending, "+");
        float newMult = 1f + (p.Stars + pending) * _services.Config.BonusPerStar;
        UIText.SetNumber(multiplierLabel, _multBuf, Mathf.RoundToInt(newMult * 100f), "x", "%");
        if (prestigeButton != null) prestigeButton.interactable = p.CanPrestige;
        if (confirmGroup != null) confirmGroup.SetActive(false);
    }

    private void AskConfirm()
    {
        if (confirmGroup != null) confirmGroup.SetActive(true);
        else DoPrestige();
    }

    private void DoPrestige()
    {
        AudioManager.Play(SfxType.LevelUp);
        GameManager.Instance.Prestige(); // Sahne yeniden yüklenir
    }
}
