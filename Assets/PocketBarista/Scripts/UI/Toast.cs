using TMPro;
using UnityEngine;

public enum ToastKind { LevelUp, ProductUnlocked, StaffHired, QuestComplete, DailyClaimed, BoostStarted, AreaUnlocked, VenueArrived }

/// <summary>
/// Ekranın üstünde kısa bildirim ("Seviye 5!", "Yeni ürün: Kek"). Tek örnek, kuyruk yok:
/// yeni bildirim eskisinin yerini alır. Metin anahtarları Localization tablosundan gelir.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class Toast : MonoBehaviour
{
    private static Toast _instance;

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text value;
    [SerializeField] private float visibleSeconds = 1.8f;

    private readonly char[] _buf = new char[32];
    private CanvasGroup _group;
    private float _remaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        _instance = this;
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }

    private void OnDestroy() { if (_instance == this) _instance = null; }

    public static void Show(ToastKind kind, double number) => _instance?.ShowInternal(kind, number, null);
    public static void Show(ToastKind kind, string text) => _instance?.ShowInternal(kind, 0, text);

    private void ShowInternal(ToastKind kind, double number, string text)
    {
        if (title != null) title.text = Localization.Get(KeyFor(kind));
        if (value != null)
        {
            if (text != null) value.text = text;
            else UIText.SetNumber(value, _buf, number);
        }
        _remaining = visibleSeconds;
        _group.alpha = 1f;
    }

    private static string KeyFor(ToastKind kind)
    {
        switch (kind)
        {
            case ToastKind.LevelUp: return "toast_level_up";
            case ToastKind.ProductUnlocked: return "toast_product_unlocked";
            case ToastKind.StaffHired: return "toast_staff_hired";
            case ToastKind.QuestComplete: return "toast_quest_complete";
            case ToastKind.DailyClaimed: return "toast_daily_claimed";
            case ToastKind.AreaUnlocked: return "toast_area_unlocked";
            case ToastKind.VenueArrived: return "toast_venue_arrived";
            default: return "toast_boost_started";
        }
    }

    private void Update()
    {
        if (_remaining <= 0f) return;
        _remaining -= Time.unscaledDeltaTime;
        _group.alpha = Mathf.Clamp01(_remaining / 0.3f);
    }
}
