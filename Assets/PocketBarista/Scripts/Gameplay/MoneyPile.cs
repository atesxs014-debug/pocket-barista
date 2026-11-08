using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Müşterinin masaya bıraktığı para (+ bahşiş). Dokununca veya kasiyer gelince toplanır.
/// Havuzlu: MoneyPilePool tarafından yönetilir. Görsel yükseklik miktara göre basamaklı açılır.
/// </summary>
[DisallowMultipleComponent]
public sealed class MoneyPile : MonoBehaviour, IPointerClickHandler
{
    private static readonly List<MoneyPile> ActivePiles = new List<MoneyPile>(32);

    [SerializeField, Tooltip("Miktar arttıkça açılan para destesi parçaları (küçükten büyüğe)")]
    private GameObject[] stackLevels = new GameObject[0];

    public long Amount { get; private set; }
    public long TipPart { get; private set; }
    public Table Table { get; private set; }
    public StaffAI ReservedBy { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActivePiles.Clear();

    /// <summary>Kasiyer için: kimsenin almadığı bir yığın.</summary>
    public static MoneyPile FindUnreserved()
    {
        for (int i = 0; i < ActivePiles.Count; i++)
            if (ActivePiles[i].ReservedBy == null) return ActivePiles[i];
        return null;
    }

    public void Setup(Table table, long amount, long tipPart)
    {
        Table = table;
        Amount = amount;
        TipPart = tipPart;
        ReservedBy = null;
        transform.position = table != null ? table.PilePosition : transform.position;

        // 1 seviye her 10 katta: 10, 100, 1K, 10K...
        int levels = Mathf.Clamp(Mathf.FloorToInt(Mathf.Log10(Mathf.Max(1f, amount))), 1, Mathf.Max(1, stackLevels.Length));
        for (int i = 0; i < stackLevels.Length; i++)
            if (stackLevels[i] != null) stackLevels[i].SetActive(i < levels);

        gameObject.SetActive(true);
        if (!ActivePiles.Contains(this)) ActivePiles.Add(this);
        if (table != null) table.AttachPile(this);
    }

    public void Collect()
    {
        if (!gameObject.activeSelf) return;

        GameServices services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        services?.CollectEarnings(Amount, TipPart);
        FloatingTextPool.ShowMoney(transform.position + Vector3.up * 0.3f, Amount);
        AudioManager.Play(SfxType.Coin);

        Table t = Table;
        Table = null;
        ReservedBy = null;
        ActivePiles.Remove(this);
        if (t != null) t.DetachPile(this);
        MoneyPilePool.Return(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ReservedBy == null) Collect();
    }

    private void OnDisable() => ActivePiles.Remove(this);
}
