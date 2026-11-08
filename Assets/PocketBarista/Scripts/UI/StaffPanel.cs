/// <summary>Personel işe alma: barista, garson, kasiyer (tek seferlik ücret, maaş yok).</summary>
public sealed class StaffPanel : ListPanelBase
{
    protected override int Count => Services.Content.Staff.Count;

    protected override void InitRow(ShopRowView row, int index)
    {
        StaffSO s = Services.Content.Staff[index];
        row.Init(index, s.Icon, Localization.Get(s.NameKey), HandleHire);
    }

    protected override void RefreshRow(ShopRowView row, int index)
    {
        StaffSO s = Services.Content.Staff[index];
        StaffService staff = Services.Staff;
        if (staff.IsHired(s)) { row.SetState(ShopRowView.RowState.Owned); return; }
        if (!staff.IsUnlocked(s)) { row.SetState(ShopRowView.RowState.Locked, s.UnlockLevel); return; }
        row.SetCost(s.HireCost);
        row.SetState(staff.CanHire(s) ? ShopRowView.RowState.Available : ShopRowView.RowState.CannotAfford);
    }

    protected override void Subscribe() => Services.Staff.StaffHired += HandleHired;
    protected override void Unsubscribe() => Services.Staff.StaffHired -= HandleHired;
    private void HandleHired(StaffSO _) => RefreshAll();

    private void HandleHire(int index) => Feedback(Services.Staff.TryHire(Services.Content.Staff[index]));
}
