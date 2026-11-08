/// <summary>Menü: yeni ürün (kek, çay, smoothie...) aç. Her ürün daha pahalı satılır.</summary>
public sealed class MenuPanel : ListPanelBase
{
    protected override int Count => Services.Content.Products.Count;

    protected override void InitRow(ShopRowView row, int index)
    {
        ProductSO p = Services.Content.Products[index];
        row.Init(index, p.Icon, Localization.Get(p.NameKey), HandleUnlock);
    }

    protected override void RefreshRow(ShopRowView row, int index)
    {
        ProductSO p = Services.Content.Products[index];
        MenuService menu = Services.Menu;
        row.SetInfoText(null);
        if (menu.IsUnlocked(p)) { row.SetState(ShopRowView.RowState.Owned); return; }
        if (!menu.IsAvailableToUnlock(p)) { row.SetState(ShopRowView.RowState.Locked, p.UnlockLevel); return; }
        row.SetCost(p.UnlockCost);
        row.SetState(menu.CanUnlock(p) ? ShopRowView.RowState.Available : ShopRowView.RowState.CannotAfford);
    }

    protected override void Subscribe() => Services.Menu.ProductUnlocked += HandleUnlocked;
    protected override void Unsubscribe() => Services.Menu.ProductUnlocked -= HandleUnlocked;
    private void HandleUnlocked(ProductSO _) => RefreshAll();

    private void HandleUnlock(int index) => Feedback(Services.Menu.TryUnlock(Services.Content.Products[index]));
}
