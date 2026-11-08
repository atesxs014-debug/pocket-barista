using UnityEngine;

/// <summary>Eşya mağazası: masa, sandalye, istasyon, dekor. Satın al → yerleştirme modu açılır.</summary>
public sealed class BuildShopPanel : ListPanelBase
{
    [SerializeField] private BuildableCatalogSO catalog;
    [SerializeField] private BuildingPlacementManager placementManager;

    protected override void Start()
    {
        if (catalog == null || placementManager == null)
        {
            Debug.LogError("[BuildShopPanel] catalog veya placementManager atanmamış.", this);
            enabled = false;
            return;
        }
        base.Start();
    }

    protected override int Count => catalog.Items.Count;

    protected override void InitRow(ShopRowView row, int index)
    {
        BuildableItemSO item = catalog.Items[index];
        string localized = Localization.Get(item.DisplayName);
        row.Init(index, item.Icon, string.IsNullOrEmpty(localized) ? item.DisplayName : localized, HandleBuy);
    }

    protected override void RefreshRow(ShopRowView row, int index)
    {
        BuildableItemSO item = catalog.Items[index];
        if (Services.Levels.Level < item.UnlockLevel) { row.SetState(ShopRowView.RowState.Locked, item.UnlockLevel); return; }
        row.SetCost(item.Price);
        row.SetState(Services.Economy.CanAfford(item.Price) ? ShopRowView.RowState.Available : ShopRowView.RowState.CannotAfford);
    }

    private void HandleBuy(int index)
    {
        // Para yerleştirme onaylanınca harcanır (BuildingPlacementManager.ConfirmPlacement)
        if (placementManager.BeginPlacement(catalog.Items[index])) Close();
        else AudioManager.Play(SfxType.Error);
    }
}
