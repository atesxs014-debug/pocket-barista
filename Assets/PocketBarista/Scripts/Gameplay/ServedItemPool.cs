using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Masaya konan ürün görselleri (fincan, tabak) için ürün başına havuz.
/// Bir ürünün ilk kullanımında prewarmPerProduct kadar oluşturulur, sonra yeniden kullanılır.
/// </summary>
[DisallowMultipleComponent]
public sealed class ServedItemPool : MonoBehaviour
{
    private static ServedItemPool _instance;

    [SerializeField] private int prewarmPerProduct = 6;

    private readonly Dictionary<ProductSO, Stack<GameObject>> _pools = new Dictionary<ProductSO, Stack<GameObject>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake() => _instance = this;
    private void OnDestroy() { if (_instance == this) _instance = null; }

    /// <summary>Ön ısıtma (yükleme ekranında çağrılabilir).</summary>
    public static void Prewarm(ProductSO product)
    {
        if (_instance == null || product == null || product.ServedPrefab == null) return;
        _instance.GetStack(product);
    }

    public static GameObject Take(ProductSO product, Transform anchor)
    {
        if (_instance == null || product == null || product.ServedPrefab == null || anchor == null) return null;
        Stack<GameObject> stack = _instance.GetStack(product);
        GameObject go = stack.Count > 0 ? stack.Pop() : _instance.CreateOne(product);
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.SetActive(true);
        return go;
    }

    public static void Give(ProductSO product, GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        if (_instance == null || product == null) { Destroy(go); return; }
        go.transform.SetParent(_instance.transform, false);
        _instance.GetStack(product).Push(go);
    }

    private Stack<GameObject> GetStack(ProductSO product)
    {
        if (_pools.TryGetValue(product, out Stack<GameObject> stack)) return stack;
        stack = new Stack<GameObject>(prewarmPerProduct);
        for (int i = 0; i < prewarmPerProduct; i++) stack.Push(CreateOne(product));
        _pools.Add(product, stack);
        return stack;
    }

    private GameObject CreateOne(ProductSO product)
    {
        GameObject go = Instantiate(product.ServedPrefab, transform);
        go.SetActive(false);
        return go;
    }
}
