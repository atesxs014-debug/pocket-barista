using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Müşteri tiplerine göre havuz oluşturur (Awake'te), seviyeye göre açık tiplerden ağırlıklı rastgele seçer.
/// Boş masa varsa müşteri oturur; yoksa kapıya kadar gelip vazgeçer (kaçırılan müşteri sayacı artar).
/// </summary>
[DisallowMultipleComponent]
public sealed class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Tooltip("Boş masa yoksa müşterinin vazgeçtiği nokta")] private Transform doorPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private int poolPerType = 8;
    [SerializeField, Tooltip("İki müşteri arası taban süre (saniye); yükseltme ile kısalır")] private float baseSpawnInterval = 4f;
    [SerializeField, Tooltip("Aynı anda en fazla kaç 'vazgeçen' müşteri yürüsün")] private int maxWalkAways = 2;
    [SerializeField] private bool logTransitions;

    public event Action<int> ActiveCountChanged;
    public int ActiveCount { get; private set; }

    private readonly Dictionary<CustomerTypeSO, CustomerAI[]> _pools = new Dictionary<CustomerTypeSO, CustomerAI[]>();
    private readonly List<CustomerTypeSO> _candidates = new List<CustomerTypeSO>(16);
    private readonly List<float> _candidateWeights = new List<float>(16);
    private readonly List<Table> _freeTables = new List<Table>(32);
    private GameServices _services;
    private float _nextSpawnTime;
    private int _walkAways;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null || spawnPoint == null || exitPoint == null)
        {
            Debug.LogError("[CustomerSpawner] GameManager/Services veya spawn/exit noktası eksik.", this);
            enabled = false;
            return;
        }
        if (doorPoint == null) doorPoint = spawnPoint;

        // Havuz: tüm tipler için başlangıçta oluşturulur
        foreach (CustomerTypeSO type in _services.Content.CustomerTypes)
        {
            if (type == null || type.Prefab == null || _pools.ContainsKey(type)) continue;
            var pool = new CustomerAI[Mathf.Max(1, poolPerType)];
            for (int i = 0; i < pool.Length; i++)
            {
                CustomerAI c = Instantiate(type.Prefab, spawnPoint.position, Quaternion.identity, transform);
                c.name = $"{type.Id}_{i:00}";
                c.gameObject.SetActive(false);
                c.Despawned += HandleDespawned;
                pool[i] = c;
            }
            _pools.Add(type, pool);
        }

        _nextSpawnTime = Time.time + 1f;
    }

    private void OnDestroy()
    {
        foreach (var pool in _pools.Values)
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null) pool[i].Despawned -= HandleDespawned;
    }

    private void Update()
    {
        if (Time.time < _nextSpawnTime) return;
        float rate = _services.Upgrades.GetMultiplier(UpgradeStat.CustomerSpawnRate);
        _nextSpawnTime = Time.time + baseSpawnInterval / Mathf.Max(0.1f, rate);
        TrySpawn();
    }

    public bool TrySpawn()
    {
        CustomerTypeSO type = PickType();
        if (type == null) return false;
        CustomerAI customer = GetPooled(type);
        if (customer == null) return false;

        Table table = PickFreeTable();
        bool ok;
        if (table != null)
        {
            ok = customer.Activate(type, table, _services, spawnPoint.position, exitPoint.position, logTransitions);
        }
        else
        {
            if (_walkAways >= maxWalkAways) return false;
            ok = customer.ActivateWalkAway(type, _services, spawnPoint.position, doorPoint.position, exitPoint.position, logTransitions);
            if (ok) _walkAways++;
        }

        if (!ok) return false;
        ActiveCount++;
        ActiveCountChanged?.Invoke(ActiveCount);
        return true;
    }

    private CustomerTypeSO PickType()
    {
        _candidates.Clear();
        _candidateWeights.Clear();
        float total = 0f;
        int level = _services.Levels.Level;
        VenueSO venue = _services.Venue.Current;
        var types = _services.Content.CustomerTypes;
        for (int i = 0; i < types.Count; i++) // for: IReadOnlyList foreach'i enumerator boxing yapar
        {
            CustomerTypeSO t = types[i];
            if (t == null || t.UnlockLevel > level || t.SpawnWeight <= 0f || !_pools.ContainsKey(t)) continue;
            float w = t.SpawnWeight * (venue != null ? venue.WeightFor(t) : 1f);
            if (w <= 0f) continue;
            _candidates.Add(t);
            _candidateWeights.Add(w);
            total += w;
        }
        if (_candidates.Count == 0) return null;

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < _candidates.Count; i++)
        {
            roll -= _candidateWeights[i];
            if (roll <= 0f) return _candidates[i];
        }
        return _candidates[_candidates.Count - 1];
    }

    private Table PickFreeTable()
    {
        _freeTables.Clear();
        IReadOnlyList<Table> all = Table.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].IsFree) _freeTables.Add(all[i]);
        return _freeTables.Count == 0 ? null : _freeTables[UnityEngine.Random.Range(0, _freeTables.Count)];
    }

    private CustomerAI GetPooled(CustomerTypeSO type)
    {
        if (!_pools.TryGetValue(type, out CustomerAI[] pool)) return null;
        for (int i = 0; i < pool.Length; i++)
            if (pool[i] != null && pool[i].State == CustomerState.Inactive && !pool[i].gameObject.activeSelf) return pool[i];
        return null;
    }

    private void HandleDespawned(CustomerAI customer)
    {
        if (ActiveCount > 0) ActiveCount--;
        ActiveCountChanged?.Invoke(ActiveCount);
    }

    // Vazgeçen müşteri sayacını günceller (sadece en az bir tane varken sayar)
    private void LateUpdate()
    {
        if (_walkAways == 0) return;
        int count = 0;
        foreach (var pool in _pools.Values)
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].State == CustomerState.WalkingAway) count++;
        _walkAways = count;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (spawnPoint != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(spawnPoint.position, 0.3f); }
        if (doorPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(doorPoint.position, 0.3f); }
        if (exitPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(exitPoint.position, 0.3f); }
    }
#endif
}
