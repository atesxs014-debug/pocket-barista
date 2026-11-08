using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tüm oyun içeriğinin tek kaydı: ürünler, müşteri tipleri, personel, yükseltmeler, görevler.
/// Kayıttaki id'ler buradan çözülür. Create > Tycoon > Game Content.
/// </summary>
[CreateAssetMenu(fileName = "GameContent", menuName = "Tycoon/Game Content")]
public sealed class GameContentSO : ScriptableObject
{
    [SerializeField] private ProductSO[] products = Array.Empty<ProductSO>();
    [SerializeField] private CustomerTypeSO[] customerTypes = Array.Empty<CustomerTypeSO>();
    [SerializeField] private StaffSO[] staff = Array.Empty<StaffSO>();
    [SerializeField] private UpgradeSO[] upgrades = Array.Empty<UpgradeSO>();
    [SerializeField, Tooltip("Sırayla verilir")] private QuestSO[] quests = Array.Empty<QuestSO>();
    [SerializeField, Tooltip("Yeniden doğuşla sırayla açılan mekanlar")] private VenueSO[] venues = Array.Empty<VenueSO>();

    public IReadOnlyList<ProductSO> Products => products;
    public IReadOnlyList<CustomerTypeSO> CustomerTypes => customerTypes;
    public IReadOnlyList<StaffSO> Staff => staff;
    public IReadOnlyList<UpgradeSO> Upgrades => upgrades;
    public IReadOnlyList<QuestSO> Quests => quests;
    public IReadOnlyList<VenueSO> Venues => venues;

    public ProductSO FindProduct(string id) => Find(products, id);
    public StaffSO FindStaff(string id) => Find(staff, id);
    public UpgradeSO FindUpgrade(string id) => Find(upgrades, id);

    // Yükleme anında çağrılır (sık değil): basit doğrusal arama yeterli
    private static T Find<T>(T[] list, string id) where T : ScriptableObject
    {
        if (list == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < list.Length; i++)
        {
            T item = list[i];
            if (item == null) continue;
            string itemId = item is ProductSO p ? p.Id : item is StaffSO s ? s.Id : item is UpgradeSO u ? u.Id : item.name;
            if (string.Equals(itemId, id, StringComparison.Ordinal)) return item;
        }
        return null;
    }

    private void OnValidate()
    {
        WarnDuplicates(products, p => p.Id);
        WarnDuplicates(staff, s => s.Id);
        WarnDuplicates(upgrades, u => u.Id);
        WarnDuplicates(quests, q => q.Id);
    }

    private void WarnDuplicates<T>(T[] list, Func<T, string> getId) where T : ScriptableObject
    {
        if (list == null) return;
        var seen = new HashSet<string>();
        foreach (T item in list)
        {
            if (item == null) continue;
            if (!seen.Add(getId(item))) Debug.LogError($"[GameContent] Tekrarlanan id: {getId(item)}", this);
        }
    }
}
