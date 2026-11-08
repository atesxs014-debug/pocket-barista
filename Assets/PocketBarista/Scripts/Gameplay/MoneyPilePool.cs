using System.Collections.Generic;
using UnityEngine;

/// <summary>Para yığını havuzu. Awake'te önceden oluşturulur.</summary>
[DisallowMultipleComponent]
public sealed class MoneyPilePool : MonoBehaviour
{
    private static MoneyPilePool _instance;

    [SerializeField] private MoneyPile prefab;
    [SerializeField] private int prewarm = 24;

    private readonly Stack<MoneyPile> _free = new Stack<MoneyPile>(32);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        if (prefab == null) { Debug.LogError("[MoneyPilePool] Prefab atanmamış.", this); enabled = false; return; }
        _instance = this;
        for (int i = 0; i < prewarm; i++) _free.Push(CreateOne());
    }

    private void OnDestroy() { if (_instance == this) _instance = null; }

    private MoneyPile CreateOne()
    {
        MoneyPile p = Instantiate(prefab, transform);
        p.gameObject.SetActive(false);
        return p;
    }

    public static MoneyPile Spawn(Table table, long amount, long tipPart)
    {
        if (_instance == null) { Debug.LogError("[MoneyPilePool] Sahnede havuz yok."); return null; }
        MoneyPile p = _instance._free.Count > 0 ? _instance._free.Pop() : _instance.CreateOne();
        p.Setup(table, amount, tipPart);
        return p;
    }

    public static void Return(MoneyPile pile)
    {
        if (pile == null) return;
        pile.gameObject.SetActive(false);
        if (_instance != null) _instance._free.Push(pile);
    }
}
