using UnityEngine;

/// <summary>Menüdeki bir ürün (espresso, kruvasan, smoothie...). Create > Tycoon > Product.</summary>
[CreateAssetMenu(fileName = "NewProduct", menuName = "Tycoon/Product")]
public sealed class ProductSO : ScriptableObject
{
    [SerializeField, Tooltip("Kayıt için sabit kimlik")] private string id;
    [SerializeField, Tooltip("Localization anahtarı, ör. product.espresso")] private string nameKey;
    [SerializeField] private Sprite icon;
    [SerializeField, Tooltip("Masaya servis edilen görsel (fincan, tabak). Havuzlanır.")] private GameObject servedPrefab;

    [Header("Ekonomi")]
    [SerializeField] private long basePrice = 10;
    [SerializeField] private long xpPerSale = 1;
    [SerializeField, Tooltip("Menüye eklemek için ödenecek para (0 = bedava)")] private long unlockCost;
    [SerializeField, Tooltip("Bu seviyeden önce menü listesinde kilitli görünür")] private int unlockLevel = 1;
    [SerializeField, Tooltip("Oyun başında açık mı?")] private bool unlockedByDefault;

    [Header("Hazırlama")]
    [SerializeField] private StationType station = StationType.Espresso;
    [SerializeField] private float prepSeconds = 3f;
    [SerializeField, Tooltip("Müşterinin yeme/içme süresi")] private float consumeSeconds = 6f;

    public string Id => id;
    public string NameKey => nameKey;
    public Sprite Icon => icon;
    public GameObject ServedPrefab => servedPrefab;
    public long BasePrice => basePrice;
    public long XPPerSale => xpPerSale;
    public long UnlockCost => unlockCost;
    public int UnlockLevel => unlockLevel;
    public bool UnlockedByDefault => unlockedByDefault;
    public StationType Station => station;
    public float PrepSeconds => prepSeconds;
    public float ConsumeSeconds => consumeSeconds;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(nameKey)) nameKey = "product." + id;
        if (basePrice < 0) basePrice = 0;
        if (xpPerSale < 0) xpPerSale = 0;
        if (unlockCost < 0) unlockCost = 0;
        if (unlockLevel < 1) unlockLevel = 1;
        if (prepSeconds < 0.1f) prepSeconds = 0.1f;
        if (consumeSeconds < 0.1f) consumeSeconds = 0.1f;
    }
}
