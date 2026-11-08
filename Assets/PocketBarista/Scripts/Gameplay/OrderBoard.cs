using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Siparişlerin merkezi: bekleyen (istasyon için) ve hazır (servis için) kuyruklar.
/// Sipariş nesneleri Awake'te havuzlanır; oyun sırasında allocation yok.
/// Sahnede tek bir tane bulunur (tezgahın yanına koyun; pickupPoint = hazır siparişlerin konduğu yer).
/// </summary>
[DisallowMultipleComponent]
public sealed class OrderBoard : MonoBehaviour
{
    public static OrderBoard Instance { get; private set; }

    [SerializeField, Tooltip("Garsonların hazır siparişi aldığı nokta (tezgah önü)")] private Transform pickupPoint;
    [SerializeField] private int poolSize = 64;

    public event Action<Order> OrderCreated;
    public event Action<Order> OrderReady;
    public event Action<Order> OrderDelivered;

    public Vector3 PickupPosition => pickupPoint != null ? pickupPoint.position : transform.position;
    public int PendingCount => _pending.Count;
    public int ReadyCount => _ready.Count;

    private readonly Stack<Order> _free = new Stack<Order>(64);
    private readonly List<Order> _pending = new List<Order>(64);
    private readonly List<Order> _ready = new List<Order>(64);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[OrderBoard] Sahnede birden fazla OrderBoard var.", this);
            enabled = false;
            return;
        }
        Instance = this;
        for (int i = 0; i < Mathf.Max(8, poolSize); i++) _free.Push(new Order());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Order Create(ProductSO product, CustomerAI customer, Table table)
    {
        if (product == null || customer == null) return null;
        if (_free.Count == 0)
        {
            Debug.LogWarning("[OrderBoard] Sipariş havuzu doldu; poolSize'ı artırın.", this);
            return null;
        }
        Order order = _free.Pop();
        order.Init(product, customer, table);
        _pending.Add(order);
        OrderCreated?.Invoke(order);
        return order;
    }

    public bool HasPending(StationType type)
    {
        for (int i = 0; i < _pending.Count; i++)
            if (_pending[i].Product.Station == type) return true;
        return false;
    }

    /// <summary>İstasyon tipine uyan en eski bekleyen siparişi kuyruktan alır.</summary>
    public Order TakeNextPending(StationType type)
    {
        for (int i = 0; i < _pending.Count; i++)
        {
            Order o = _pending[i];
            if (o.Product.Station != type) continue;
            _pending.RemoveAt(i);
            return o;
        }
        return null;
    }

    /// <summary>İstasyon kapanırsa/bozulursa sipariş kuyruğun başına döner.</summary>
    public void ReturnToPending(Order order)
    {
        if (order == null || order.State == OrderState.Free || order.State == OrderState.Cancelled) return;
        order.State = OrderState.Pending;
        order.Station = null;
        _pending.Insert(0, order);
    }

    public void MarkReady(Order order)
    {
        if (order == null || order.State != OrderState.Preparing) return;
        order.State = OrderState.Ready;
        order.Station = null;
        _ready.Add(order);
        OrderReady?.Invoke(order);
    }

    /// <summary>Garson için: kimsenin almadığı en eski hazır sipariş.</summary>
    public Order ClaimNextReady(StaffAI courier)
    {
        for (int i = 0; i < _ready.Count; i++)
        {
            Order o = _ready[i];
            if (o.Courier != null) continue;
            o.Courier = courier;
            return o;
        }
        return null;
    }

    /// <summary>Oyuncu masaya dokununca: o masa için hazır ve sahipsiz sipariş.</summary>
    public Order FindReadyForTable(Table table)
    {
        for (int i = 0; i < _ready.Count; i++)
        {
            Order o = _ready[i];
            if (o.Table == table && o.Courier == null) return o;
        }
        return null;
    }

    /// <summary>Siparişi müşteriye teslim eder. Müşteri artık beklemiyorsa false.</summary>
    public bool Deliver(Order order)
    {
        if (order == null || order.State != OrderState.Ready) return false;
        _ready.Remove(order);

        if (order.Customer == null || !order.Customer.ReceiveOrder(order))
        {
            Release(order);
            return false;
        }

        order.State = OrderState.Delivered;
        order.Courier = null;
        OrderDelivered?.Invoke(order);
        return true;
    }

    /// <summary>Müşteri vazgeçti: sipariş hangi aşamadaysa iptal edilir.</summary>
    public void Cancel(Order order)
    {
        if (order == null || order.State == OrderState.Free) return;
        _pending.Remove(order);
        _ready.Remove(order);
        if (order.Station != null) order.Station.AbortCurrent(returnToQueue: false);
        Release(order);
    }

    /// <summary>Tüketim bitti veya iptal: havuza geri.</summary>
    public void Release(Order order)
    {
        if (order == null || order.State == OrderState.Free) return;
        order.Clear();
        _free.Push(order);
    }
}
