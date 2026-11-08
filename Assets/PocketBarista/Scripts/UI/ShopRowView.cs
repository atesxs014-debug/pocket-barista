using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tüm listelerde (yükseltme, menü, personel, eşya) kullanılan ortak satır prefab'ı.
/// Paneller veriyi buna basar; satır sadece gösterir ve tıklamayı bildirir.
/// </summary>
public sealed class ShopRowView : MonoBehaviour
{
    public enum RowState { Locked, CannotAfford, Available, Owned, Maxed }

    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text infoLabel;      // "Sv 3/10" veya "+%20 gelir"
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedLabel;    // "Seviye 5"
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color availableColor = new Color(0.95f, 0.62f, 0.25f);
    [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.32f);

    private readonly char[] _costBuf = new char[32];
    private readonly char[] _infoBuf = new char[32];
    private readonly char[] _lockBuf = new char[32];
    private Action<int> _onClick; // null olabilir (sadece bilgi satırı)
    private int _index;

    public void Init(int index, Sprite sprite, string displayName, Action<int> onClick)
    {
        _index = index;
        _onClick = onClick;
        if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
        if (nameLabel != null) nameLabel.text = displayName; // Bir kez, açılışta
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetName(string displayName) { if (nameLabel != null) nameLabel.text = displayName; }

    public void SetCost(long cost) => UIText.SetNumber(costLabel, _costBuf, cost, "$");
    public void SetCostText(string text) { if (costLabel != null) costLabel.text = text; }

    public void SetInfoLevel(int level, int max)
    {
        if (infoLabel == null) return;
        UIText.SetProgress(infoLabel, _infoBuf, level, max);
    }

    public void SetInfoText(string text) { if (infoLabel != null) infoLabel.text = text; }

    public void SetState(RowState state, int requiredLevel = 0)
    {
        bool locked = state == RowState.Locked;
        if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        if (locked && lockedLabel != null) UIText.SetNumber(lockedLabel, _lockBuf, requiredLevel, "Lv ");
        if (button != null) button.interactable = state == RowState.Available;
        if (buttonImage != null) buttonImage.color = state == RowState.Available ? availableColor : disabledColor;
        if (state == RowState.Maxed) SetCostText("MAX");
        else if (state == RowState.Owned) SetCostText("✓");
    }

    private void HandleClick() => _onClick?.Invoke(_index);
}
