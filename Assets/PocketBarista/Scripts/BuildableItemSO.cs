using UnityEngine;

/// <summary>
/// Mağazada satılan, sahneye yerleştirilebilen bir eşyanın verisi (masa, dekor vb.).
/// Create > Tycoon > Buildable Item ile oluşturun.
/// </summary>
[CreateAssetMenu(fileName = "NewBuildable", menuName = "Tycoon/Buildable Item")]
public sealed class BuildableItemSO : ScriptableObject
{
    [Header("Kimlik")]
    [SerializeField, Tooltip("Kayıt sistemi için sabit, benzersiz kimlik. Yayından sonra değiştirmeyin.")]
    private string id;

    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 4)] private string description;

    [SerializeField] private Sprite icon;

    [Header("Ekonomi")]
    [SerializeField] private long price = 100;
    [SerializeField, Tooltip("Mağazada açılması için gereken oyuncu seviyesi")] private int unlockLevel = 1;
    [SerializeField, Tooltip("Yerleştirince kazanılan XP (dükkan prestiji)")] private long xpOnPlace = 10;

    public enum ShopCategory { Seating, Station, Decor, Floor }
    [SerializeField] private ShopCategory category = ShopCategory.Decor;

    [Header("Prefab'lar")]
    [SerializeField, Tooltip("Satın alınınca sahneye konan asıl obje")]
    private GameObject prefab;

    [SerializeField, Tooltip("Sadece görsel önizleme (script/collider içermeyen). Boşsa asıl prefab kullanılır.")]
    private GameObject previewPrefab;

    [Header("Yerleşim")]
    [SerializeField, Tooltip("Izgara hücresi cinsinden kapladığı alan (X, Z)")]
    private Vector2Int footprint = Vector2Int.one;

    [SerializeField, Tooltip("Kapalıysa serbest yerleştirilir (yine de footprint ile çakışma kontrolü yapılır)")]
    private bool snapToGrid = true;

    [SerializeField, Tooltip("Açıksa satın aldıktan sonra yerleştirme modu kapanmaz (çoklu dekor dizmek için)")]
    private bool keepPlacingAfterBuild;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public long Price => price;
    public int UnlockLevel => unlockLevel;
    public long XpOnPlace => xpOnPlace;
    public ShopCategory Category => category;
    public GameObject Prefab => prefab;
    public GameObject PreviewPrefab => previewPrefab;
    public Vector2Int Footprint => footprint;
    public bool SnapToGrid => snapToGrid;
    public bool KeepPlacingAfterBuild => keepPlacingAfterBuild;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
        if (price < 0) price = 0;
        if (unlockLevel < 1) unlockLevel = 1;
        if (xpOnPlace < 0) xpOnPlace = 0;
        if (footprint.x < 1) footprint.x = 1;
        if (footprint.y < 1) footprint.y = 1;
        if (prefab == null) Debug.LogWarning($"[BuildableItemSO] {name}: prefab atanmamış.", this);
    }
}
