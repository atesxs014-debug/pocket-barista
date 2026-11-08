using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum CustomerState
{
    Inactive,     // Havuzda
    Entering,     // Masaya yürüyor
    Ordering,     // Menüye bakıyor (kısa düşünme)
    Waiting,      // Siparişini bekliyor (sabır sayacı)
    Consuming,    // Yiyor/içiyor
    Leaving,      // Çıkışa yürüyor
    WalkingAway   // Boş masa yoktu: kapıya kadar gelip vazgeçiyor
}

/// <summary>
/// FSM tabanlı müşteri. Akış:
/// Entering → Ordering → Waiting → Consuming → (tekrar Ordering | para yığını bırak → Leaving) → Inactive.
/// Sabır biterse sipariş iptal, o ana kadarki hesap yine masaya bırakılır.
/// Havuzlu: CustomerSpawner yönetir. Update içinde allocation yok.
/// </summary>
[DisallowMultipleComponent]
public sealed class CustomerAI : MonoBehaviour
{
    [SerializeField] private float baseMoveSpeed = 2.2f;
    [SerializeField] private float basePatienceSeconds = 20f;
    [SerializeField] private float thinkSeconds = 1.2f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField, Tooltip("Elde taşınan ürün için nokta (opsiyonel)")] private Transform handAnchor;

    public event Action<CustomerAI, CustomerState, CustomerState> StateChanged;
    public event Action<CustomerAI> Despawned;
    public event Action<CustomerAI> LeftUnhappy;

    public CustomerState State { get; private set; } = CustomerState.Inactive;
    public CustomerTypeSO Type { get; private set; }
    public Order CurrentOrder { get; private set; }
    public ProductSO WaitingFor => CurrentOrder != null ? CurrentOrder.Product : null;
    public float PatienceNormalized => State == CustomerState.Waiting && _patienceMax > 0f ? Mathf.Clamp01(_timer / _patienceMax) : 1f;

    private Transform _transform;
    private Table _table;
    private GameServices _services;
    private Vector3 _exitPosition;
    private Vector3 _doorPosition;
    private float _timer;
    private float _patienceMax;
    private float _moveSpeed;
    private int _ordersLeft;
    private long _bill;
    private long _tips;
    private GameObject _servedVisual;
    private ProductSO _servedProduct;
    private int _orderVersion;
    private bool _logTransitions;

    private void Awake() => _transform = transform;

    // ---------------- Aktivasyon ----------------

    public bool Activate(CustomerTypeSO type, Table table, GameServices services, Vector3 spawn, Vector3 exit, bool log)
    {
        if (State != CustomerState.Inactive || type == null || table == null || services == null) return false;
        if (!table.TryReserve(this)) return false;

        Setup(type, services, spawn, exit, log);
        _table = table;
        _ordersLeft = UnityEngine.Random.Range(type.MinOrders, type.MaxOrders + 1);
        gameObject.SetActive(true);
        ChangeState(CustomerState.Entering);
        return true;
    }

    /// <summary>Boş masa yokken: kapıya kadar gelir, vazgeçip gider (kaçırılan müşteri).</summary>
    public bool ActivateWalkAway(CustomerTypeSO type, GameServices services, Vector3 spawn, Vector3 door, Vector3 exit, bool log)
    {
        if (State != CustomerState.Inactive || type == null || services == null) return false;
        Setup(type, services, spawn, exit, log);
        _doorPosition = door;
        gameObject.SetActive(true);
        ChangeState(CustomerState.WalkingAway);
        return true;
    }

    private void Setup(CustomerTypeSO type, GameServices services, Vector3 spawn, Vector3 exit, bool log)
    {
        Type = type;
        _services = services;
        _exitPosition = exit;
        _logTransitions = log;
        _bill = 0;
        _tips = 0;
        _moveSpeed = baseMoveSpeed * type.MoveSpeedMultiplier;
        _transform.position = spawn;
    }

    // ---------------- Dış çağrılar ----------------

    /// <summary>OrderBoard.Deliver çağırır.</summary>
    public bool ReceiveOrder(Order order)
    {
        if (State != CustomerState.Waiting || order == null || order != CurrentOrder || order.Version != _orderVersion) return false;

        ProductSO product = order.Product;
        long price = _services.PriceFor(product, Type);
        long tip = _services.RollTip(price, Type);
        _bill = SaturatingAdd(_bill, SaturatingAdd(price, tip));
        _tips = SaturatingAdd(_tips, tip);
        _services.Economy.AddXP(product.XPPerSale);

        _servedProduct = product;
        _servedVisual = ServedItemPool.Take(product, _table != null ? _table.ItemAnchor : handAnchor);

        ChangeState(CustomerState.Consuming);
        return true;
    }

    public void ForceLeave()
    {
        if (State == CustomerState.Inactive || State == CustomerState.Leaving || State == CustomerState.WalkingAway) return;
        CancelCurrentOrder();
        FinishVisit(paid: _bill > 0);
    }

    // ---------------- FSM ----------------

    private void Update()
    {
        float dt = Time.deltaTime;
        switch (State)
        {
            case CustomerState.Entering:
                if (_table == null) { FinishVisit(false); break; }
                if (MoveTo(_table.SeatPosition, dt))
                {
                    _transform.rotation = _table.SeatRotation;
                    ChangeState(CustomerState.Ordering);
                }
                break;

            case CustomerState.Ordering:
                _timer -= dt;
                if (_timer <= 0f) PlaceOrder();
                break;

            case CustomerState.Waiting:
                _timer -= dt;
                if (_timer <= 0f)
                {
                    LeftUnhappy?.Invoke(this);
                    CancelCurrentOrder();
                    FinishVisit(paid: _bill > 0);
                }
                break;

            case CustomerState.Consuming:
                _timer -= dt;
                if (_timer <= 0f) FinishConsuming();
                break;

            case CustomerState.WalkingAway:
                if (MoveTo(_doorPosition, dt))
                {
                    _services.Stats.RecordCustomerLost();
                    ChangeState(CustomerState.Leaving);
                }
                break;

            case CustomerState.Leaving:
                if (MoveTo(_exitPosition, dt)) Despawn();
                break;
        }
    }

    private void PlaceOrder()
    {
        OrderBoard board = OrderBoard.Instance;
        ProductSO product = _services.Menu.PickRandomOrder();
        if (board == null || product == null)
        {
            // Menüde uygun ürün/istasyon yok
            FinishVisit(paid: _bill > 0);
            return;
        }

        CurrentOrder = board.Create(product, this, _table);
        if (CurrentOrder == null) { FinishVisit(paid: _bill > 0); return; }
        _orderVersion = CurrentOrder.Version;
        ChangeState(CustomerState.Waiting);
    }

    private void FinishConsuming()
    {
        ReturnServedVisual();
        if (OrderBoard.Instance != null && CurrentOrder != null && CurrentOrder.Version == _orderVersion)
            OrderBoard.Instance.Release(CurrentOrder);
        CurrentOrder = null;

        _ordersLeft--;
        if (_ordersLeft > 0) ChangeState(CustomerState.Ordering);
        else FinishVisit(paid: true);
    }

    private void FinishVisit(bool paid)
    {
        if (paid && _bill > 0 && _table != null)
        {
            MoneyPilePool.Spawn(_table, _bill, _tips);
            _services.Stats.RecordCustomerServed();
        }
        ChangeState(CustomerState.Leaving);
    }

    private void CancelCurrentOrder()
    {
        if (CurrentOrder != null && CurrentOrder.Version == _orderVersion && OrderBoard.Instance != null)
            OrderBoard.Instance.Cancel(CurrentOrder);
        CurrentOrder = null;
        ReturnServedVisual();
    }

    private void ReturnServedVisual()
    {
        if (_servedVisual != null) ServedItemPool.Give(_servedProduct, _servedVisual);
        _servedVisual = null;
        _servedProduct = null;
    }

    private void Despawn()
    {
        ChangeState(CustomerState.Inactive);
        gameObject.SetActive(false);
        Despawned?.Invoke(this);
    }

    private void OnDisable()
    {
        // Dışarıdan kapatılırsa (sahne kapanışı) kaynakları bırak
        if (State == CustomerState.Inactive) return;
        CancelCurrentOrder();
        ReleaseTable();
        State = CustomerState.Inactive;
        Despawned?.Invoke(this);
    }

    private void ChangeState(CustomerState next)
    {
        CustomerState previous = State;
        if (previous == next) return;
        State = next;

        switch (next)
        {
            case CustomerState.Ordering:
                _timer = thinkSeconds;
                break;
            case CustomerState.Waiting:
                _patienceMax = basePatienceSeconds * Type.PatienceMultiplier * _services.Upgrades.GetMultiplier(UpgradeStat.CustomerPatience);
                _timer = _patienceMax;
                break;
            case CustomerState.Consuming:
                _timer = _servedProduct != null
                    ? _servedProduct.ConsumeSeconds / _services.Upgrades.GetMultiplier(UpgradeStat.EatSpeed)
                    : 1f;
                break;
            case CustomerState.Leaving:
                ReleaseTable(); // Masa para yığını toplanınca tamamen boşalır
                break;
            case CustomerState.Inactive:
                _services = null;
                Type = null;
                break;
        }

        if (_logTransitions) LogTransition(name, previous, next);
        StateChanged?.Invoke(this, previous, next);
    }

    private void ReleaseTable()
    {
        if (_table != null) _table.Release(this);
        _table = null;
    }

    private bool MoveTo(Vector3 target, float dt)
    {
        Vector3 current = _transform.position;
        Vector3 next = Vector3.MoveTowards(current, target, _moveSpeed * dt);
        Vector3 dir = next - current;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.000001f) _transform.rotation = Quaternion.LookRotation(dir);
        _transform.position = next;
        return (next - target).sqrMagnitude <= arriveDistance * arriveDistance;
    }

    private static long SaturatingAdd(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    private static void LogTransition(string who, CustomerState from, CustomerState to) =>
        Debug.Log($"[CustomerAI] {who}: {from} → {to}");
}
