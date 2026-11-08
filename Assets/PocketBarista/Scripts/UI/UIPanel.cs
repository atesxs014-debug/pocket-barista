using UnityEngine;

/// <summary>
/// Açılır/kapanır panel: CanvasGroup + ölçek animasyonu (unscaled time, coroutine yok, GC yok).
/// Alt paneller (mağaza, yükseltme vb.) bunu kullanır; butonlardan Open/Close/Toggle bağlanır.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [SerializeField] private RectTransform window;
    [SerializeField] private float animSeconds = 0.18f;
    [SerializeField] private bool startOpen;

    private CanvasGroup _group;
    private float _t;
    private bool _open;

    public bool IsOpen => _open;
    public event System.Action<bool> OpenChanged;

    protected virtual void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (window == null) window = (RectTransform)transform;
        _open = startOpen;
        _t = _open ? 1f : 0f;
        Apply();
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!_open);

    public void SetOpen(bool open)
    {
        if (_open == open) return;
        _open = open;
        if (open) { gameObject.SetActive(true); OnOpened(); }
        AudioManager.Play(SfxType.Tap);
        OpenChanged?.Invoke(open);
    }

    protected virtual void OnOpened() { }

    protected virtual void Update()
    {
        float target = _open ? 1f : 0f;
        if (Mathf.Approximately(_t, target)) return;
        _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, animSeconds));
        Apply();
    }

    private void Apply()
    {
        if (_group == null) return;
        _group.alpha = _t;
        _group.interactable = _open;
        _group.blocksRaycasts = _open;
        float eased = 1f - (1f - _t) * (1f - _t);
        window.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, eased);
    }
}
