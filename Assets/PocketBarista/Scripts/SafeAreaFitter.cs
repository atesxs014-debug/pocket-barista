using UnityEngine;

/// <summary>
/// UI panelini cihazın güvenli alanına (çentik, kamera deliği, alt çubuk) sığdırır.
/// Canvas'ın altındaki tam ekran bir "SafeArea" objesine ekleyin; tüm HUD onun çocuğu olsun.
/// Sadece değişiklik olduğunda hesaplar, her kare allocation yapmaz.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect _lastSafeArea;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // Ekran döndürme / pencere boyutu değişimi
        if (Screen.safeArea != _lastSafeArea || Screen.width != _lastWidth || Screen.height != _lastHeight)
            Apply();
    }

    private void Apply()
    {
        int width = Screen.width;
        int height = Screen.height;
        if (width <= 0 || height <= 0) return;

        Rect safe = Screen.safeArea;
        _lastSafeArea = safe;
        _lastWidth = width;
        _lastHeight = height;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= width; min.y /= height;
        max.x /= width; max.y /= height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
