using TMPro;
using UnityEngine;

/// <summary>
/// Para veya XP'yi gösteren UI. Sadece okur ve dinler; ekonomiyi değiştirmez.
/// Sayıyı önceden ayrılmış char buffer'a yazar: her güncellemede string üretmez (GC yok).
/// </summary>
public sealed class CurrencyView : MonoBehaviour
{
    private enum CurrencyKind { Money, XP, Stars }

    [SerializeField] private TMP_Text label;
    [SerializeField] private CurrencyKind kind = CurrencyKind.Money;
    [SerializeField] private string prefix = "$";

    // long.MaxValue 19 hane + 6 ayraç + önek için yeterli
    private readonly char[] _buffer = new char[48];
    private PlayerEconomy _economy;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label == null)
        {
            Debug.LogError($"[CurrencyView] {name}: TMP_Text bulunamadı.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null || GameManager.Instance.Economy == null)
        {
            Debug.LogError("[CurrencyView] GameManager hazır değil. Bootstrap sahnesini kontrol edin.", this);
            return;
        }

        _economy = GameManager.Instance.Economy;

        if (kind == CurrencyKind.Money)
        {
            _economy.MoneyChanged += Render;
            Render(_economy.Money); // Geç abone olsa bile güncel değeri çiz
        }
        else if (kind == CurrencyKind.XP)
        {
            _economy.XPChanged += Render;
            Render(_economy.XP);
        }
        else
        {
            Render(GameManager.Instance.Services != null ? GameManager.Instance.Services.Prestige.Stars : 0);
        }
    }

    private void OnDisable()
    {
        if (_economy == null) return;
        _economy.MoneyChanged -= Render;
        _economy.XPChanged -= Render;
        _economy = null;
    }

    private void Render(long value)
    {
        // Idle formatı: 1.23K, 4.50M, 7.00B, aa, ab... (GC yok)
        int len = 0;
        for (int i = 0; i < prefix.Length && len < 8; i++) _buffer[len++] = prefix[i];
        len += BigNumber.Write(value, _buffer, len);
        label.SetCharArray(_buffer, 0, len);
    }
}
