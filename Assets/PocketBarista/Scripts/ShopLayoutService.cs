using System;
using UnityEngine;

/// <summary>
/// Dükkan düzeninin kalıcılığı: açılışta kayıtlı eşyaları geri yükler, yeni oyuncu için başlangıç
/// dükkanını kurar ve oyuncunun satın aldığı her eşyayı kayda ekler.
/// Veri GameManager'daki SaveData'da tutulur; bu sınıf sadece sahne ile kayıt arasında köprüdür.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopLayoutService : MonoBehaviour
{
    [Serializable]
    private struct StartingItem
    {
        public BuildableItemSO item;
        public Vector3 position;
        [Range(0, 3)] public int rotationSteps;
    }

    [SerializeField] private BuildableCatalogSO catalog;
    [SerializeField] private BuildingPlacementManager placementManager;

    [SerializeField, Tooltip("Yeni oyuncunun ücretsiz başladığı eşyalar (ör. 1 masa + 1 tezgah)")]
    private StartingItem[] startingLayout = Array.Empty<StartingItem>();

    private GameManager _gameManager;
    private bool _subscribed;

    private void Start()
    {
        _gameManager = GameManager.Instance;
        if (_gameManager == null || catalog == null || placementManager == null)
        {
            Debug.LogError("[ShopLayout] GameManager, catalog veya placementManager eksik. Düzen yüklenmedi.", this);
            enabled = false;
            return;
        }

        // Sıra önemli: önce kayıttakiler, sonra (sadece ilk açılışta) başlangıç dükkanı
        RestoreSavedLayout();
        if (!_gameManager.IsLayoutInitialized) PlaceStartingLayout();

        placementManager.ItemPlaced += HandleItemPlaced;
        _subscribed = true;
    }

    private void OnDestroy()
    {
        if (_subscribed && placementManager != null) placementManager.ItemPlaced -= HandleItemPlaced;
    }

    private void RestoreSavedLayout()
    {
        var items = _gameManager.PlacedItems;
        int restored = 0;

        for (int i = 0; i < items.Count; i++)
        {
            PlacedItemData data = items[i];
            if (!catalog.TryGet(data.itemId, out BuildableItemSO item))
            {
                // Kayıttan silinmez: eşya katalogdan geçici olarak çıkarıldıysa sonraki sürümde geri gelir
                Debug.LogWarning($"[ShopLayout] Katalogda olmayan eşya atlandı: '{data.itemId}'", this);
                continue;
            }

            if (placementManager.SpawnWithoutCost(item, new Vector3(data.x, 0f, data.z), data.rotationSteps) != null)
                restored++;
        }

        Debug.Log($"[ShopLayout] {restored}/{items.Count} eşya geri yüklendi.", this);
    }

    private void PlaceStartingLayout()
    {
        for (int i = 0; i < startingLayout.Length; i++)
        {
            StartingItem start = startingLayout[i];
            if (start.item == null) continue;

            GameObject go = placementManager.SpawnWithoutCost(start.item, start.position, start.rotationSteps);
            if (go != null) _gameManager.AddPlacedItem(ToData(start.item, go.transform));
        }

        _gameManager.MarkLayoutInitialized();
        Debug.Log($"[ShopLayout] Başlangıç dükkanı kuruldu ({startingLayout.Length} eşya).", this);
    }

    private void HandleItemPlaced(BuildableItemSO item, GameObject placed)
    {
        if (item == null || placed == null) return;
        _gameManager.AddPlacedItem(ToData(item, placed.transform));

        // Oyuncu satın aldı: istatistik (görevler) + dükkan prestiji XP'si
        GameServices services = _gameManager.Services;
        if (services == null) return;
        services.Stats.RecordItemPlaced();
        if (item.XpOnPlace > 0) services.Economy.AddXP(item.XpOnPlace);
        AudioManager.Play(SfxType.Place);
    }

    private static PlacedItemData ToData(BuildableItemSO item, Transform t)
    {
        Vector3 p = t.position;
        return new PlacedItemData
        {
            itemId = item.Id,
            x = p.x,
            z = p.z,
            rotationSteps = Mathf.RoundToInt(t.eulerAngles.y / 90f) & 3
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.62f, 0.2f, 0.8f);
        for (int i = 0; i < startingLayout.Length; i++)
            Gizmos.DrawWireCube(startingLayout[i].position + Vector3.up * 0.5f, Vector3.one);
    }
#endif
}
