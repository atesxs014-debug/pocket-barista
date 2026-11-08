using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Üst barda seviye rozeti + XP dolum çubuğu. Seviye atlayınca ses ve bildirim.</summary>
public sealed class LevelView : MonoBehaviour
{
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private Image fill;               // Image Type = Filled
    [SerializeField] private float fillLerpSpeed = 4f;

    private readonly char[] _buf = new char[16];
    private GameServices _services;
    private float _shownFill;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        _services.Levels.LevelChanged += HandleLevel;
        _shownFill = _services.Levels.Progress01;
        RenderLevel(_services.Levels.Level);
    }

    private void OnDestroy()
    {
        if (_services == null) return;
        _services.Levels.LevelChanged -= HandleLevel;
    }

    private void HandleLevel(int level)
    {
        RenderLevel(level);
        _shownFill = 0f;
        AudioManager.Play(SfxType.LevelUp);
        Toast.Show(ToastKind.LevelUp, level);
    }

    private void RenderLevel(int level) => UIText.SetNumber(levelLabel, _buf, level);

    private void Update()
    {
        if (fill == null) return;
        float target = _services.Levels.Progress01;
        if (Mathf.Approximately(_shownFill, target)) return;
        _shownFill = Mathf.MoveTowards(_shownFill, target, fillLerpSpeed * Time.unscaledDeltaTime);
        fill.fillAmount = _shownFill;
    }
}
