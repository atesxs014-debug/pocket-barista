using System;
using UnityEngine;

/// <summary>Cihaz tercihleri (ses, müzik, titreşim). Oyun kaydından bağımsız, PlayerPrefs'te.</summary>
public static class GameSettings
{
    private const string SfxKey = "PB_Sfx";
    private const string MusicKey = "PB_Music";
    private const string HapticsKey = "PB_Haptics";

    public static event Action Changed;

    public static bool SfxOn => PlayerPrefs.GetInt(SfxKey, 1) == 1;
    public static bool MusicOn => PlayerPrefs.GetInt(MusicKey, 1) == 1;
    public static bool HapticsOn => PlayerPrefs.GetInt(HapticsKey, 1) == 1;

    public static void SetSfx(bool on) => Set(SfxKey, on);
    public static void SetMusic(bool on) => Set(MusicKey, on);
    public static void SetHaptics(bool on) => Set(HapticsKey, on);

    private static void Set(string key, bool on)
    {
        PlayerPrefs.SetInt(key, on ? 1 : 0);
        PlayerPrefs.Save(); // Nadir olay
        Changed?.Invoke();
    }
}
