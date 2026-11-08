using UnityEngine;

/// <summary>Mekan sahnesi açılınca kısa "Hoş geldin: Sahil Kafesi" bildirimi gösterir ve ortam rengini uygular.</summary>
public sealed class VenueArrivalBanner : MonoBehaviour
{
    private void Start()
    {
        GameServices s = GameManager.Instance != null ? GameManager.Instance.Services : null;
        VenueSO v = s?.Venue.Current;
        if (v == null) return;
        RenderSettings.ambientLight = v.AmbientColor;
        Toast.Show(ToastKind.VenueArrived, Localization.Get(v.NameKey));
    }
}
