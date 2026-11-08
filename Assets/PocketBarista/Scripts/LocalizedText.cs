using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro etiketini anahtara göre aktif dilde gösterir ve dil değişince günceller.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField, Tooltip("LocalizationTable'daki anahtar, ör. ui.shop.title")]
    private string key;

    private TMP_Text _label;

    private void Awake() => _label = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        Localization.LanguageChanged += HandleLanguageChanged;
        Refresh();
    }

    private void OnDisable() => Localization.LanguageChanged -= HandleLanguageChanged;

    /// <summary>Kod içinden anahtar değiştirmek için (ör. mağazada seçilen eşyanın adı).</summary>
    public void SetKey(string newKey)
    {
        if (newKey == key) return;
        key = newKey;
        Refresh();
    }

    private void HandleLanguageChanged(GameLanguage _) => Refresh();

    private void Refresh()
    {
        if (_label == null || string.IsNullOrEmpty(key)) return;
        _label.text = Localization.Get(key);
    }
}
