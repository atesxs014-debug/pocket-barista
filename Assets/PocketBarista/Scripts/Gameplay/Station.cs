using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hazırlama istasyonu (espresso makinesi, fırın, çay, blender). Yerleştirilebilir prefab'a eklenir.
/// Oyuncu dokununca sıradaki siparişi başlatır; hazırlanırken dokunmak süreyi kısaltır (tap-to-speed).
/// Barista personeli de aynı API ile çalışır.
/// </summary>
[DisallowMultipleComponent]
public sealed class Station : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private StationType type = StationType.Espresso;
    [SerializeField, Tooltip("Baristanın durduğu nokta")] private Transform workPoint;
    [SerializeField, Tooltip("Hazırlanırken her dokunuşta eklenen ilerleme (0.15 = %15)")] private float tapBoost = 0.15f;

    public event Action<Station> PrepStarted;
    public event Action<Station> PrepCompleted;

    public StationType Type => type;
    public bool IsIdle => _current == null;
    public StaffAI ReservedBy { get; set; }
    public float Progress01 => _current == null ? 0f : Mathf.Clamp01(_progress);
    public Vector3 WorkPosition => workPoint != null ? workPoint.position : transform.position + transform.forward * -0.8f;

    private Order _current;
    private int _currentVersion;
    private float _progress;
    private float _workerSpeed = 1f;

    private void OnEnable() => StationRegistry.Register(this);

    private void OnDisable()
    {
        StationRegistry.Unregister(this);
        AbortCurrent(returnToQueue: true);
        ReservedBy = null;
    }

    public bool TryStart(Order order, float workerSpeed)
    {
        if (!IsIdle || order == null || order.State != OrderState.Pending || order.Product.Station != type) return false;
        _current = order;
        _currentVersion = order.Version;
        order.State = OrderState.Preparing;
        order.Station = this;
        _progress = 0f;
        _workerSpeed = Mathf.Max(0.1f, workerSpeed);
        PrepStarted?.Invoke(this);
        return true;
    }

    private void Update()
    {
        if (_current == null) return;

        // Sipariş iptal edilip havuza döndüyse
        if (_current.Version != _currentVersion || _current.State != OrderState.Preparing)
        {
            _current = null;
            return;
        }

        GameServices services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        float prepMul = services != null ? services.Upgrades.GetMultiplier(UpgradeStat.PrepSpeed) : 1f;
        _progress += Time.deltaTime * _workerSpeed * prepMul / _current.Product.PrepSeconds;

        if (_progress >= 1f) Complete();
    }

    private void Complete()
    {
        Order done = _current;
        _current = null;
        _progress = 0f;
        if (OrderBoard.Instance != null) OrderBoard.Instance.MarkReady(done);
        AudioManager.Play(SfxType.PrepDone);
        PrepCompleted?.Invoke(this);
    }

    /// <summary>İstasyon kaldırılırsa veya sipariş iptal edilirse.</summary>
    public void AbortCurrent(bool returnToQueue)
    {
        if (_current == null) return;
        Order o = _current;
        _current = null;
        _progress = 0f;
        if (returnToQueue && o.Version == _currentVersion && OrderBoard.Instance != null) OrderBoard.Instance.ReturnToPending(o);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_current != null)
        {
            _progress += tapBoost;          // Oyuncu hızlandırıyor
            AudioManager.Play(SfxType.Tap);
            if (_progress >= 1f) Complete();
            return;
        }

        if (ReservedBy != null || OrderBoard.Instance == null) return;
        Order next = OrderBoard.Instance.TakeNextPending(type);
        if (next != null && !TryStart(next, 1f)) OrderBoard.Instance.ReturnToPending(next);
    }
}
