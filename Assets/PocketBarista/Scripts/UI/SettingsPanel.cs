using UnityEngine;
using UnityEngine.UI;

/// <summary>Ayarlar: ses, müzik, titreşim, dil (TR/EN).</summary>
public sealed class SettingsPanel : UIPanel
{
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle hapticsToggle;
    [SerializeField] private Button turkishButton;
    [SerializeField] private Button englishButton;

    private void Start()
    {
        Bind(sfxToggle, GameSettings.SfxOn, GameSettings.SetSfx);
        Bind(musicToggle, GameSettings.MusicOn, GameSettings.SetMusic);
        Bind(hapticsToggle, GameSettings.HapticsOn, GameSettings.SetHaptics);
        if (turkishButton != null) turkishButton.onClick.AddListener(() => Localization.SetLanguage(GameLanguage.Turkish));
        if (englishButton != null) englishButton.onClick.AddListener(() => Localization.SetLanguage(GameLanguage.English));
    }

    private static void Bind(Toggle toggle, bool value, System.Action<bool> setter)
    {
        if (toggle == null) return;
        toggle.SetIsOnWithoutNotify(value);
        toggle.onValueChanged.AddListener(v => setter(v));
    }
}
