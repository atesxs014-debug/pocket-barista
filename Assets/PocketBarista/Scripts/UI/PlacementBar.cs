using UnityEngine;

/// <summary>Yerleştirme modundayken görünen "Onayla / Döndür / İptal" çubuğu. Butonlar Inspector'dan bağlanır.</summary>
public sealed class PlacementBar : MonoBehaviour
{
    [SerializeField] private BuildingPlacementManager placementManager;
    [SerializeField] private GameObject bar;

    private void Start()
    {
        if (placementManager == null || bar == null) { enabled = false; return; }
        placementManager.PlacementStarted += HandleStarted;
        placementManager.PlacementEnded += HandleEnded;
        bar.SetActive(placementManager.IsPlacing);
    }

    private void OnDestroy()
    {
        if (placementManager == null) return;
        placementManager.PlacementStarted -= HandleStarted;
        placementManager.PlacementEnded -= HandleEnded;
    }

    private void HandleStarted(BuildableItemSO _) => bar.SetActive(true);
    private void HandleEnded() => bar.SetActive(false);

    public void Confirm() => placementManager.ConfirmPlacement();
    public void Rotate() => placementManager.RotatePreview();
    public void Cancel() => placementManager.CancelPlacement();
}
