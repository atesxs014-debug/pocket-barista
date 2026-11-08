using System;
using UnityEngine;

/// <summary>
/// Aktif dil ve metin erişimi. GameManager.Awake içinde Initialize edilir.
/// Dil tercihi cihaz ayarıdır, oyun kaydından bağımsız olarak PlayerPrefs'te tutulur.
/// Get() tablodaki string referansını döndürür, yeni string üretmez.
/// </summary>
public static class Localization
{
    private const string LanguagePrefKey = "PocketBarista_Language";

    /// <summary>Dil değişince tüm LocalizedText'ler yenilenir.</summary>
    public static event Action<GameLanguage> LanguageChanged;

    public static GameLanguage Current { get; private set; } = GameLanguage.Turkish;
    public static bool IsReady => _table != null;

    private static LocalizationTableSO _table;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _table = null;
        LanguageChanged = null;
        Current = GameLanguage.Turkish;
    }

    public static void Initialize(LocalizationTableSO table)
    {
        if (table == null)
        {
            Debug.LogError("[Localization] Tablo atanmamış; metinler anahtar olarak görünecek.");
            return;
        }

        _table = table;

        if (PlayerPrefs.HasKey(LanguagePrefKey))
        {
            int saved = PlayerPrefs.GetInt(LanguagePrefKey, 0);
            Current = Enum.IsDefined(typeof(GameLanguage), saved) ? (GameLanguage)saved : GameLanguage.Turkish;
        }
        else
        {
            // İlk açılış: cihaz dili Türkçe ise Türkçe, değilse İngilizce
            Current = Application.systemLanguage == SystemLanguage.Turkish ? GameLanguage.Turkish : GameLanguage.English;
        }

        Debug.Log($"[Localization] Dil: {Current}");
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (language == Current) return;
        Current = language;
        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save(); // Nadir bir olay; disk yazımı burada sorun değil
        LanguageChanged?.Invoke(language);
    }

    /// <summary>Anahtar bulunamazsa anahtarın kendisini döndürür (eksik metin ekranda görünür).</summary>
    public static string Get(string key)
    {
        if (_table != null && _table.TryGet(key, Current, out string value)) return value;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[Localization] Eksik anahtar: '{key}'");
#endif
        return key;
    }
}
