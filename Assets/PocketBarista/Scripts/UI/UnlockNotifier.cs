using UnityEngine;

/// <summary>Servis olaylarını Toast bildirimlerine çevirir (ürün açıldı, personel alındı).</summary>
public sealed class UnlockNotifier : MonoBehaviour
{
    private GameServices _services;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        _services.Menu.ProductUnlocked += HandleProduct;
        _services.Staff.StaffHired += HandleStaff;
    }

    private void OnDestroy()
    {
        if (_services == null) return;
        _services.Menu.ProductUnlocked -= HandleProduct;
        _services.Staff.StaffHired -= HandleStaff;
    }

    private void HandleProduct(ProductSO p) => Toast.Show(ToastKind.ProductUnlocked, Localization.Get(p.NameKey));
    private void HandleStaff(StaffSO s) => Toast.Show(ToastKind.StaffHired, Localization.Get(s.NameKey));
}
