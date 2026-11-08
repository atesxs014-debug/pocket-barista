using TMPro;
using UnityEngine;

/// <summary>Uçan "+$35" metni. FloatingTextPool tarafından yönetilir; GC üretmez.</summary>
[DisallowMultipleComponent]
public sealed class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.6f, 0.2f, 1f);

    private readonly char[] _buffer = new char[24];
    private Transform _transform;
    private Transform _camera;
    private float _age;
    private Color _color;

    private void Awake()
    {
        _transform = transform;
        if (label == null) label = GetComponentInChildren<TMP_Text>();
    }

    public void Show(Vector3 position, long amount, Color color, Transform cameraTransform)
    {
        _transform.position = position;
        _camera = cameraTransform;
        _age = 0f;
        _color = color;

        int len = 0;
        _buffer[len++] = '+';
        _buffer[len++] = '$';
        len += BigNumber.Write(amount, _buffer, len);
        label.SetCharArray(_buffer, 0, len);
        label.color = color;
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        _age += Time.deltaTime;
        float t = _age / lifetime;
        if (t >= 1f) { FloatingTextPool.Return(this); return; }

        _transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        if (_camera != null) _transform.rotation = _camera.rotation; // Kameraya dön (billboard)
        _transform.localScale = Vector3.one * scaleCurve.Evaluate(_age);

        Color c = _color;
        c.a = 1f - t * t;
        label.color = c;
    }
}
