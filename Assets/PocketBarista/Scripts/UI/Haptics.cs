using UnityEngine;

/// <summary>Basit titreşim. Ayarlardan kapatılabilir.</summary>
public static class Haptics
{
    public static void Light()
    {
        if (!GameSettings.HapticsOn) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
