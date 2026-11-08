using UnityEngine;

/// <summary>İşe alınabilir personel. Bir kerelik ücret, maaş yok. Create > Tycoon > Staff.</summary>
[CreateAssetMenu(fileName = "NewStaff", menuName = "Tycoon/Staff")]
public sealed class StaffSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string nameKey;
    [SerializeField] private Sprite icon;
    [SerializeField] private StaffAI prefab;
    [SerializeField] private StaffRole role = StaffRole.Barista;
    [SerializeField] private long hireCost = 500;
    [SerializeField] private int unlockLevel = 1;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField, Tooltip("Barista için hazırlama hız çarpanı")] private float workSpeed = 1f;

    public string Id => id;
    public string NameKey => nameKey;
    public Sprite Icon => icon;
    public StaffAI Prefab => prefab;
    public StaffRole Role => role;
    public long HireCost => hireCost;
    public int UnlockLevel => unlockLevel;
    public float MoveSpeed => moveSpeed;
    public float WorkSpeed => workSpeed;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(nameKey)) nameKey = "staff." + id;
        if (hireCost < 0) hireCost = 0;
        if (unlockLevel < 1) unlockLevel = 1;
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        workSpeed = Mathf.Max(0.1f, workSpeed);
    }
}
