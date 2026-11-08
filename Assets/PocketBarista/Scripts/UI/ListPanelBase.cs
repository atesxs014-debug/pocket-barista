using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Liste panellerinin ortak tabanı: satırları açılışta BİR KEZ oluşturur, para/seviye değişince
/// sadece durumları yeniler (allocation yok). Alt sınıflar sadece veri bağlamayı yazar.
/// </summary>
public abstract class ListPanelBase : UIPanel
{
    [SerializeField] protected ShopRowView rowPrefab;
    [SerializeField] protected RectTransform content;

    protected readonly List<ShopRowView> Rows = new List<ShopRowView>(16);
    protected GameServices Services;
    private bool _built;

    protected virtual void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Services == null || rowPrefab == null || content == null)
        {
            Debug.LogError($"[{GetType().Name}] GameManager/rowPrefab/content eksik.", this);
            enabled = false;
            return;
        }
        Services = gm.Services;
        BuildRows();
        Services.Economy.MoneyChanged += HandleMoneyChanged;
        Services.Levels.LevelChanged += HandleLevelChanged;
        Localization.LanguageChanged += HandleLanguageChanged;
        Subscribe();
        RefreshAll();
    }

    protected virtual void OnDestroy()
    {
        if (Services == null) return;
        Services.Economy.MoneyChanged -= HandleMoneyChanged;
        Services.Levels.LevelChanged -= HandleLevelChanged;
        Localization.LanguageChanged -= HandleLanguageChanged;
        Unsubscribe();
    }

    private void BuildRows()
    {
        if (_built) return;
        _built = true;
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            ShopRowView row = Instantiate(rowPrefab, content);
            InitRow(row, i);
            Rows.Add(row);
        }
    }

    protected override void OnOpened() { if (Services != null) RefreshAll(); }

    protected void RefreshAll()
    {
        for (int i = 0; i < Rows.Count; i++) RefreshRow(Rows[i], i);
    }

    private void HandleMoneyChanged(long _) { if (IsOpen) RefreshAll(); }
    private void HandleLevelChanged(int _) => RefreshAll();
    private void HandleLanguageChanged(GameLanguage _) { for (int i = 0; i < Rows.Count; i++) InitRow(Rows[i], i); RefreshAll(); }

    protected abstract int Count { get; }
    protected abstract void InitRow(ShopRowView row, int index);
    protected abstract void RefreshRow(ShopRowView row, int index);
    protected virtual void Subscribe() { }
    protected virtual void Unsubscribe() { }

    protected static void Feedback(bool success)
    {
        AudioManager.Play(success ? SfxType.Purchase : SfxType.Error);
        if (success && GameSettings.HapticsOn) Haptics.Light();
    }
}
