using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mekan içinde parayla açılan bölge (teras, üst kat, bahçe...). Kilitliyken engel (çit/duvar + "Satılık" tabelası) görünür,
/// açılınca içerik (masalar, dekor) aktif olur ve yerleştirme alanı genişler. Tabelaya dokununca satın alınır.
/// </summary>
[DisallowMultipleComponent]
public sealed class ExpansionArea : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Tooltip("Mekan içinde benzersiz. Kayıtta tutulur.")] private string areaId;
    [SerializeField] private string nameKey;
    [SerializeField] private long cost = 1000;
    [SerializeField] private int requiredLevel = 1;

    [Header("Sahne")]
    [SerializeField, Tooltip("Kilitliyken görünen: çit, perde, tabela")] private GameObject lockedRoot;
    [SerializeField, Tooltip("Açılınca görünen: zemin, masalar, dekor (Table'lar OnEnable'da kendini kaydeder)")] private GameObject unlockedRoot;
    [SerializeField, Tooltip("Açılınca yerleştirme yapılabilecek alan (opsiyonel)")] private Collider buildableGround;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private ParticleSystem unlockEffect;

    private readonly char[] _priceBuf = new char[32];
    private readonly char[] _levelBuf = new char[16];
    private GameServices _services;

    public string AreaId => areaId;
    public bool IsUnlocked => _services != null && _services.Venue.IsAreaUnlocked(areaId);

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null || string.IsNullOrEmpty(areaId))
        {
            Debug.LogError("[ExpansionArea] Servis yok veya areaId boş.", this);
            enabled = false;
            return;
        }
        _services.Venue.AreaUnlocked += HandleUnlocked;
        _services.Economy.MoneyChanged += HandleMoney;
        _services.Levels.LevelChanged += HandleLevel;
        Apply(playEffect: false);
    }

    private void OnDestroy()
    {
        if (_services == null) return;
        _services.Venue.AreaUnlocked -= HandleUnlocked;
        _services.Economy.MoneyChanged -= HandleMoney;
        _services.Levels.LevelChanged -= HandleLevel;
    }

    public void OnPointerClick(PointerEventData eventData) => TryBuy();

    /// <summary>UI butonundan da çağrılabilir.</summary>
    public void TryBuy()
    {
        if (_services == null || IsUnlocked) return;
        bool ok = _services.Venue.TryUnlockArea(areaId, cost, requiredLevel);
        AudioManager.Play(ok ? SfxType.Purchase : SfxType.Error);
        if (ok) Toast.Show(ToastKind.AreaUnlocked, Localization.Get(nameKey));
    }

    private void HandleUnlocked(string id) { if (id == areaId) Apply(playEffect: true); }
    private void HandleMoney(long _) { if (!IsUnlocked) RefreshLabels(); }
    private void HandleLevel(int _) { if (!IsUnlocked) RefreshLabels(); }

    private void Apply(bool playEffect)
    {
        bool unlocked = IsUnlocked;
        if (lockedRoot != null) lockedRoot.SetActive(!unlocked);
        if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
        if (buildableGround != null) buildableGround.enabled = unlocked;
        if (unlocked && playEffect && unlockEffect != null) unlockEffect.Play();
        if (!unlocked) RefreshLabels();
    }

    private void RefreshLabels()
    {
        UIText.SetNumber(priceLabel, _priceBuf, cost, "$");
        if (priceLabel != null) priceLabel.color = _services.Economy.CanAfford(cost) ? Color.white : new Color(1f, 0.45f, 0.4f);
        bool levelOk = _services.Levels.Level >= requiredLevel;
        if (levelLabel != null)
        {
            levelLabel.gameObject.SetActive(!levelOk);
            if (!levelOk) UIText.SetNumber(levelLabel, _levelBuf, requiredLevel, "Lv ");
        }
    }

    private void OnValidate()
    {
        if (cost < 0) cost = 0;
        if (requiredLevel < 1) requiredLevel = 1;
    }
}
