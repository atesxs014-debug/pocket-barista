using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyundaki tüm yerleştirilebilir eşyaların listesi. Kayıttaki itemId'den BuildableItemSO bulmak için kullanılır.
/// Mağaza UI'ı da eşya listesini buradan okur. Create > Tycoon > Buildable Catalog.
/// </summary>
[CreateAssetMenu(fileName = "BuildableCatalog", menuName = "Tycoon/Buildable Catalog")]
public sealed class BuildableCatalogSO : ScriptableObject
{
    [SerializeField] private BuildableItemSO[] items = Array.Empty<BuildableItemSO>();

    private Dictionary<string, BuildableItemSO> _lookup;

    public IReadOnlyList<BuildableItemSO> Items => items;

    public bool TryGet(string id, out BuildableItemSO item)
    {
        item = null;
        if (string.IsNullOrEmpty(id)) return false;
        EnsureLookup();
        return _lookup.TryGetValue(id, out item) && item != null;
    }

    private void OnEnable() => _lookup = null;   // Domain reload kapalıyken eski sözlük kalmasın

    private void OnValidate()
    {
        _lookup = null;
        EnsureLookup(); // Tekrarlanan id'leri editörde hemen uyar
    }

    private void EnsureLookup()
    {
        if (_lookup != null) return;

        _lookup = new Dictionary<string, BuildableItemSO>(items != null ? items.Length : 0, StringComparer.Ordinal);
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            BuildableItemSO item = items[i];
            if (item == null) continue;

            if (_lookup.ContainsKey(item.Id))
            {
                Debug.LogError($"[BuildableCatalog] Tekrarlanan id: '{item.Id}' ({item.name}). Kayıt yüklemesi bozulabilir!", this);
                continue;
            }
            _lookup.Add(item.Id, item);
        }
    }
}
