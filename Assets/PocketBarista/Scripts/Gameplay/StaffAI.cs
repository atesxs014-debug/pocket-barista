using System;
using UnityEngine;

/// <summary>
/// Personel FSM'i. Rol davranışları:
/// Barista: bekleyen siparişi olan boş istasyona gider, hazırlar.
/// Garson: hazır siparişi tezgahtan alır, masaya götürür.
/// Kasiyer: masalardaki para yığınlarını toplar.
/// Hareket MoveTowards ile; Update'te allocation yok. Görev araması 0.25 sn'de bir yapılır.
/// </summary>
[DisallowMultipleComponent]
public sealed class StaffAI : MonoBehaviour
{
    private enum State { Idle, ToStation, Preparing, ToPickup, ToTable, ToPile, ReturningHome }

    [SerializeField] private float arriveDistance = 0.08f;
    [SerializeField] private float scanInterval = 0.25f;
    [SerializeField, Tooltip("Garsonun elinde ürün göstermek için")] private Transform handAnchor;

    public event Action<StaffAI> TaskCompleted;

    public StaffSO Data { get; private set; }
    public StaffRole Role => Data != null ? Data.Role : StaffRole.Barista;

    private Transform _transform;
    private GameServices _services;
    private Vector3 _home;
    private State _state = State.Idle;
    private float _scanTimer;

    private Station _station;
    private Order _order;
    private int _orderVersion;
    private MoneyPile _pile;
    private GameObject _carriedVisual;
    private ProductSO _carriedProduct;

    private void Awake() => _transform = transform;

    public void Init(StaffSO data, GameServices services, Vector3 home)
    {
        Data = data;
        _services = services;
        _home = home;
        _transform.position = home;
        _state = State.Idle;
    }

    private void OnDisable() => AbandonTask();

    private void Update()
    {
        if (Data == null || _services == null) return;
        float dt = Time.deltaTime;

        switch (_state)
        {
            case State.Idle:
                _scanTimer -= dt;
                if (_scanTimer <= 0f)
                {
                    _scanTimer = scanInterval;
                    if (TryFindTask()) break;
                }
                if ((_transform.position - _home).sqrMagnitude > 0.04f) _state = State.ReturningHome;
                break;

            case State.ReturningHome:
                if (MoveTo(_home, dt)) _state = State.Idle;
                _scanTimer -= dt;
                if (_scanTimer <= 0f) { _scanTimer = scanInterval; TryFindTask(); }
                break;

            case State.ToStation:
                if (_station == null || !_station.isActiveAndEnabled) { AbandonTask(); break; }
                if (MoveTo(_station.WorkPosition, dt)) StartPreparing();
                break;

            case State.Preparing:
                if (_station == null || _station.IsIdle) { ReleaseStation(); Finish(); }
                break;

            case State.ToPickup:
                if (!OrderStillMine()) { AbandonTask(); break; }
                if (MoveTo(OrderBoard.Instance.PickupPosition, dt))
                {
                    _carriedProduct = _order.Product;
                    _carriedVisual = ServedItemPool.Take(_carriedProduct, handAnchor != null ? handAnchor : _transform);
                    _state = State.ToTable;
                }
                break;

            case State.ToTable:
                if (!OrderStillMine() || _order.Table == null) { AbandonTask(); break; }
                if (MoveTo(_order.Table.ServicePosition, dt))
                {
                    DropCarried();
                    _order.Courier = null;
                    if (OrderBoard.Instance.Deliver(_order)) AudioManager.Play(SfxType.Serve);
                    _order = null;
                    Finish();
                }
                break;

            case State.ToPile:
                if (_pile == null || !_pile.gameObject.activeSelf || _pile.ReservedBy != this) { _pile = null; Finish(); break; }
                Vector3 target = _pile.Table != null ? _pile.Table.ServicePosition : _pile.transform.position;
                if (MoveTo(target, dt))
                {
                    _pile.ReservedBy = null;
                    _pile.Collect();
                    _pile = null;
                    Finish();
                }
                break;
        }
    }

    private bool TryFindTask()
    {
        OrderBoard board = OrderBoard.Instance;
        switch (Data.Role)
        {
            case StaffRole.Barista:
                if (board == null) return false;
                _station = StationRegistry.FindWorkable(board);
                if (_station == null) return false;
                _station.ReservedBy = this;
                _state = State.ToStation;
                return true;

            case StaffRole.Waiter:
                if (board == null) return false;
                _order = board.ClaimNextReady(this);
                if (_order == null) return false;
                _orderVersion = _order.Version;
                _state = State.ToPickup;
                return true;

            case StaffRole.Cashier:
                _pile = MoneyPile.FindUnreserved();
                if (_pile == null) return false;
                _pile.ReservedBy = this;
                _state = State.ToPile;
                return true;
        }
        return false;
    }

    private void StartPreparing()
    {
        Order next = OrderBoard.Instance != null ? OrderBoard.Instance.TakeNextPending(_station.Type) : null;
        if (next == null || !_station.TryStart(next, Data.WorkSpeed))
        {
            if (next != null) OrderBoard.Instance.ReturnToPending(next);
            ReleaseStation();
            Finish();
            return;
        }
        _state = State.Preparing;
    }

    private bool OrderStillMine() =>
        _order != null && _order.Version == _orderVersion && _order.State == OrderState.Ready && _order.Courier == this && OrderBoard.Instance != null;

    private void Finish()
    {
        _state = State.Idle;
        _scanTimer = 0f;
        TaskCompleted?.Invoke(this);
    }

    private void AbandonTask()
    {
        DropCarried();
        ReleaseStation();
        if (_order != null && _order.Version == _orderVersion && _order.Courier == this) _order.Courier = null;
        _order = null;
        if (_pile != null && _pile.ReservedBy == this) _pile.ReservedBy = null;
        _pile = null;
        _state = State.Idle;
    }

    private void ReleaseStation()
    {
        if (_station != null && _station.ReservedBy == this) _station.ReservedBy = null;
        _station = null;
    }

    private void DropCarried()
    {
        if (_carriedVisual != null) ServedItemPool.Give(_carriedProduct, _carriedVisual);
        _carriedVisual = null;
        _carriedProduct = null;
    }

    private bool MoveTo(Vector3 target, float dt)
    {
        float speed = Data.MoveSpeed * _services.Upgrades.GetMultiplier(UpgradeStat.StaffMoveSpeed);
        Vector3 current = _transform.position;
        Vector3 next = Vector3.MoveTowards(current, target, speed * dt);
        Vector3 dir = next - current;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.000001f) _transform.rotation = Quaternion.LookRotation(dir);
        _transform.position = next;
        return (next - target).sqrMagnitude <= arriveDistance * arriveDistance;
    }
}
