using TMPro;
using UnityEngine;

/// <summary>"$12.5/sn" gelir hızı göstergesi (IncomeTracker'ın EMA değeri). Saniyede bir günceller.</summary>
public sealed class IncomeRateView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string suffix = "/s";

    private readonly char[] _buf = new char[32];
    private GameServices _services;
    private float _next;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null || label == null) enabled = false;
    }

    private void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 1f;
        UIText.SetNumber(label, _buf, _services.Income.IncomePerSecond, "$", suffix);
    }
}
