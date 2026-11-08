/// <summary>Yükseltme mağazası: gelir, bahşiş, hız, sabır, müşteri sıklığı, çevrimdışı süre...</summary>
public sealed class UpgradePanel : ListPanelBase
{
    protected override int Count => Services.Content.Upgrades.Count;

    protected override void InitRow(ShopRowView row, int index)
    {
        UpgradeSO u = Services.Content.Upgrades[index];
        row.Init(index, u.Icon, Localization.Get(u.NameKey), HandleBuy);
    }

    protected override void RefreshRow(ShopRowView row, int index)
    {
        UpgradeSO u = Services.Content.Upgrades[index];
        UpgradeService up = Services.Upgrades;
        row.SetInfoLevel(up.GetLevel(u), u.MaxLevel);

        if (!up.IsUnlocked(u)) { row.SetState(ShopRowView.RowState.Locked, u.UnlockLevel); return; }
        if (up.IsMaxed(u)) { row.SetState(ShopRowView.RowState.Maxed); return; }
        row.SetCost(up.GetNextCost(u));
        row.SetState(up.CanBuy(u) ? ShopRowView.RowState.Available : ShopRowView.RowState.CannotAfford);
    }

    protected override void Subscribe() => Services.Upgrades.UpgradeChanged += HandleChanged;
    protected override void Unsubscribe() => Services.Upgrades.UpgradeChanged -= HandleChanged;
    private void HandleChanged(UpgradeSO _, int __) => RefreshAll();

    private void HandleBuy(int index) => Feedback(Services.Upgrades.TryBuy(Services.Content.Upgrades[index]));
}
