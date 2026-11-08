using UnityEngine;

/// <summary>
/// Müşteri tipi: görsel ve davranış çarpanları. Create > Tycoon > Customer Type.
/// Örn. öğrenci: sabırsız, ucuz; iş insanı: cömert bahşiş; mahalle sakini: sadık, sabırlı.
/// </summary>
[CreateAssetMenu(fileName = "NewCustomerType", menuName = "Tycoon/Customer Type")]
public sealed class CustomerTypeSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string nameKey;
    [SerializeField, Tooltip("CustomerAI içeren prefab")] private CustomerAI prefab;

    [Header("Gelme")]
    [SerializeField, Tooltip("Rastgele seçimde ağırlık")] private float spawnWeight = 1f;
    [SerializeField] private int unlockLevel = 1;

    [Header("Davranış")]
    [SerializeField] private float patienceMultiplier = 1f;
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField, Tooltip("Ödediği fiyat çarpanı")] private float priceMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float tipChance = 0.3f;
    [SerializeField, Tooltip("Bahşiş = fiyat x bu oran")] private float tipRatio = 0.2f;
    [SerializeField, Tooltip("Bir oturuşta kaç sipariş verir (min-max)")] private Vector2Int ordersPerVisit = new Vector2Int(1, 1);

    public string Id => id;
    public string NameKey => nameKey;
    public CustomerAI Prefab => prefab;
    public float SpawnWeight => spawnWeight;
    public int UnlockLevel => unlockLevel;
    public float PatienceMultiplier => patienceMultiplier;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float PriceMultiplier => priceMultiplier;
    public float TipChance => tipChance;
    public float TipRatio => tipRatio;
    public int MinOrders => ordersPerVisit.x;
    public int MaxOrders => ordersPerVisit.y;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(nameKey)) nameKey = "customer." + id;
        if (spawnWeight < 0f) spawnWeight = 0f;
        if (unlockLevel < 1) unlockLevel = 1;
        patienceMultiplier = Mathf.Max(0.1f, patienceMultiplier);
        moveSpeedMultiplier = Mathf.Max(0.1f, moveSpeedMultiplier);
        priceMultiplier = Mathf.Max(0f, priceMultiplier);
        tipRatio = Mathf.Max(0f, tipRatio);
        ordersPerVisit.x = Mathf.Max(1, ordersPerVisit.x);
        ordersPerVisit.y = Mathf.Max(ordersPerVisit.x, ordersPerVisit.y);
    }
}
