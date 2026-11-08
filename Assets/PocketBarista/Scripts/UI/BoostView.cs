using TMPro;
using UnityEngine;

/// <summary>Aktif gelir boost'u göstergesi (x2 + geri sayım). Boost günlük ödül ve görevlerden gelir.</summary>
public sealed class BoostView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text timerLabel;

    private readonly char[] _buf = new char[16];
    private GameServices _services;
    private long _lastShownSecond = -1;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        _services.Boost.BoostChanged += HandleChanged;
    }

    private void OnDestroy() { if (_services != null) _services.Boost.BoostChanged -= HandleChanged; }

    private void HandleChanged() { Toast.Show(ToastKind.BoostStarted, (long)(_services.Boost.RemainingSeconds / 60)); _lastShownSecond = -1; }

    private void Update()
    {
        bool active = _services.Boost.IsActive;
        if (root != null && root.activeSelf != active) root.SetActive(active);
        if (!active) return;

        long sec = (long)_services.Boost.RemainingSeconds;
        if (sec == _lastShownSecond) return; // Saniyede bir yaz
        _lastShownSecond = sec;
        UIText.SetDuration(timerLabel, _buf, sec);
    }
}
