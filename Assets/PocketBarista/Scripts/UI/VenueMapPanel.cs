using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mekan haritası: 5 mekanı sırayla gösterir (geçilmiş / şu an / sıradaki / kilitli).
/// "Taşın" butonu = yeniden doğuş: sonraki mekana geçilir, yıldızlar kalır.
/// </summary>
public sealed class VenueMapPanel : ListPanelBase
{
    [SerializeField] private TMP_Text currentVenueLabel;
    [SerializeField] private TMP_Text pendingStarsLabel;
    [SerializeField] private Button moveButton;
    [SerializeField] private GameObject confirmGroup;
    [SerializeField] private Button confirmButton;

    private readonly char[] _starsBuf = new char[32];

    protected override void Start()
    {
        base.Start();
        if (Services == null) return;
        if (moveButton != null) moveButton.onClick.AddListener(AskConfirm);
        if (confirmButton != null) confirmButton.onClick.AddListener(Move);
    }

    protected override int Count => Services.Content.Venues.Count;

    protected override void InitRow(ShopRowView row, int index)
    {
        VenueSO v = Services.Content.Venues[index];
        row.Init(index, v.Icon, Localization.Get(v.NameKey), null);
    }

    protected override void RefreshRow(ShopRowView row, int index)
    {
        VenueSO v = Services.Content.Venues[index];
        int current = Services.Venue.Index;
        row.SetInfoText(Localization.Get(v.DescriptionKey));
        if (index < current) { row.SetState(ShopRowView.RowState.Owned); return; }
        if (index == current) { row.SetState(ShopRowView.RowState.CannotAfford); row.SetCostText(Localization.Get("ui.venue.here")); return; }
        if (index == current + 1) { row.SetState(ShopRowView.RowState.CannotAfford); row.SetCostText(Localization.Get("ui.venue.next")); return; }
        row.SetState(ShopRowView.RowState.Locked, 0);
        row.SetCostText("?");
    }

    protected override void OnOpened()
    {
        base.OnOpened();
        if (Services == null) return;
        VenueSO cur = Services.Venue.Current;
        if (currentVenueLabel != null && cur != null) currentVenueLabel.text = Localization.Get(cur.NameKey);
        UIText.SetNumber(pendingStarsLabel, _starsBuf, Services.Prestige.PendingStars, "+");
        if (moveButton != null) moveButton.interactable = Services.Prestige.CanPrestige;
        if (confirmGroup != null) confirmGroup.SetActive(false);
    }

    private void AskConfirm()
    {
        if (confirmGroup != null) confirmGroup.SetActive(true);
        else Move();
    }

    private void Move()
    {
        AudioManager.Play(SfxType.LevelUp);
        GameManager.Instance.Prestige(); // Sonraki mekana taşınır ve sahne yüklenir
    }
}
