using System.Collections.Generic;
using UnityEngine;

/// <summary>Uçan sayı havuzu. Sahnede bir tane; statik ShowMoney her yerden çağrılır.</summary>
[DisallowMultipleComponent]
public sealed class FloatingTextPool : MonoBehaviour
{
    private static FloatingTextPool _instance;

    [SerializeField] private FloatingText prefab;
    [SerializeField] private int prewarm = 16;
    [SerializeField] private Color moneyColor = new Color(1f, 0.78f, 0.3f);

    private readonly Stack<FloatingText> _free = new Stack<FloatingText>(16);
    private Transform _camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        if (prefab == null) { Debug.LogError("[FloatingTextPool] Prefab atanmamış.", this); enabled = false; return; }
        _instance = this;
        _camera = Camera.main != null ? Camera.main.transform : null;
        for (int i = 0; i < prewarm; i++) _free.Push(CreateOne());
    }

    private void OnDestroy() { if (_instance == this) _instance = null; }

    private FloatingText CreateOne()
    {
        FloatingText ft = Instantiate(prefab, transform);
        ft.gameObject.SetActive(false);
        return ft;
    }

    public static void ShowMoney(Vector3 worldPos, long amount)
    {
        if (_instance == null || amount <= 0) return;
        FloatingText ft = _instance._free.Count > 0 ? _instance._free.Pop() : _instance.CreateOne();
        ft.Show(worldPos, amount, _instance.moneyColor, _instance._camera);
    }

    public static void Return(FloatingText ft)
    {
        if (ft == null) return;
        ft.gameObject.SetActive(false);
        if (_instance != null) _instance._free.Push(ft);
    }
}
