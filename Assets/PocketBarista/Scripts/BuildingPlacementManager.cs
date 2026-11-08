using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PlacementFailReason
{
    None,
    NoItem,           // Eşya/prefab yok veya yerleştirme modunda değil
    NotReady,         // GameManager/Economy hazır değil
    NotEnoughMoney,
    InvalidPosition   // Alan dışında veya başka objeyle çakışıyor
}

/// <summary>
/// Dokunmatik yerleştirme: BeginPlacement → tek parmakla sürükle → Rotate / Confirm / Cancel.
/// Önizleme her eşya için bir kez oluşturulup önbelleğe alınır; Update içinde allocation yoktur.
/// Asıl obje sadece satın alma onaylandığında Instantiate edilir (oyun döngüsünde değil, tek seferlik olay).
/// Not: Legacy Input kullanır. Player Settings > Active Input Handling = "Input Manager (Old)" veya "Both" olmalı.
/// </summary>
[DisallowMultipleComponent]
public sealed class BuildingPlacementManager : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField, Tooltip("Boşsa Camera.main kullanılır")] private Camera worldCamera;
    [SerializeField, Tooltip("Yerleştirilen objelerin parent'ı. Boşsa bu obje.")] private Transform placedItemsRoot;

    [Header("Zemin ve Izgara")]
    [SerializeField] private float groundHeight = 0f;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    [SerializeField, Tooltip("Yerleştirilebilir alan (dünya koordinatı, XZ kullanılır)")]
    private Bounds buildArea = new Bounds(Vector3.zero, new Vector3(20f, 10f, 20f));

    [Header("Çakışma")]
    [SerializeField, Tooltip("Engel sayılan katmanlar. Zemin katmanını DAHİL ETMEYİN.")]
    private LayerMask blockingLayers = ~0;
    [SerializeField] private float overlapHeight = 2f;
    [SerializeField, Tooltip("Yan yana objelerin temasının çakışma sayılmaması için küçültme payı")]
    private float overlapPadding = 0.05f;

    [Header("Önizleme Renkleri")]
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);
    [SerializeField, Tooltip("URP Lit: _BaseColor, Built-in Standard: _Color")]
    private string colorProperty = "_BaseColor";

    public event Action<BuildableItemSO> PlacementStarted;
    public event Action PlacementEnded;
    /// <summary>Onay butonunu aktif/pasif yapmak için.</summary>
    public event Action<bool> ValidityChanged;
    public event Action<BuildableItemSO, GameObject> ItemPlaced;
    public event Action<BuildableItemSO, PlacementFailReason> PlacementFailed;

    public bool IsPlacing => _currentItem != null;
    public bool IsValid { get; private set; }
    public BuildableItemSO CurrentItem => _currentItem;

    private sealed class PreviewCache
    {
        public GameObject Root;
        public Transform Transform;
        public Renderer[] Renderers;
    }

    private readonly Collider[] _overlapBuffer = new Collider[8];
    private readonly Dictionary<BuildableItemSO, PreviewCache> _previews = new Dictionary<BuildableItemSO, PreviewCache>(8);

    private MaterialPropertyBlock _propertyBlock;
    private int _colorId;
    private Plane _groundPlane;
    private Transform _previewParent;
    private PlayerEconomy _economy;

    private BuildableItemSO _currentItem;
    private PreviewCache _preview;
    private Vector3 _position;
    private int _rotationSteps;        // 0..3 → 0°, 90°, 180°, 270°
    private int _activeFingerId = -1;
    private bool _mouseBlockedByUI;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null)
        {
            Debug.LogError("[Placement] Kamera bulunamadı. Sistem kapatıldı.", this);
            enabled = false;
            return;
        }

        if (placedItemsRoot == null) placedItemsRoot = transform;
        if (cellSize <= 0f) cellSize = 1f;

        _propertyBlock = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(colorProperty);
        _groundPlane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));

        var holder = new GameObject("PlacementPreviews");
        _previewParent = holder.transform;
        _previewParent.SetParent(transform, false);
    }

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.Economy == null)
        {
            Debug.LogError("[Placement] GameManager/Economy bulunamadı.", this);
            return;
        }
        _economy = GameManager.Instance.Economy;
    }

    private void OnValidate()
    {
        if (cellSize < 0.01f) cellSize = 0.01f;
        if (overlapHeight < 0.01f) overlapHeight = 0.01f;
        if (overlapPadding < 0f) overlapPadding = 0f;
    }

    private void OnDisable()
    {
        if (IsPlacing) CancelPlacement();
    }

    private void Update()
    {
        if (!IsPlacing) return;
        HandleInput();
    }

    // ---------------- Public API (UI butonları buraya bağlanır) ----------------

    /// <summary>Mağaza butonu: yerleştirme modunu başlatır. Para yetmiyorsa hemen reddeder.</summary>
    public bool BeginPlacement(BuildableItemSO item)
    {
        if (item == null || item.Prefab == null) { Fail(item, PlacementFailReason.NoItem); return false; }
        if (_economy == null) { Fail(item, PlacementFailReason.NotReady); return false; }
        if (!_economy.CanAfford(item.Price)) { Fail(item, PlacementFailReason.NotEnoughMoney); return false; }

        if (IsPlacing) EndPlacement();

        _currentItem = item;
        _rotationSteps = 0;
        _activeFingerId = -1;
        _preview = GetOrCreatePreview(item);
        _preview.Root.SetActive(true);

        // Başlangıç: ekranın ortasının zemindeki karşılığı
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 start = TryGetGroundPoint(screenCenter, out Vector3 p) ? p : buildArea.center;
        SetPosition(start, force: true);

        Debug.Log($"[Placement] Başladı: {item.DisplayName} ({item.Price})");
        PlacementStarted?.Invoke(item);
        return true;
    }

    /// <summary>Döndür butonu: 90° döndürür.</summary>
    public void RotatePreview()
    {
        if (!IsPlacing) return;
        _rotationSteps = (_rotationSteps + 1) & 3;
        SetPosition(_position, force: true); // Footprint değişti: yeniden hizala ve doğrula
    }

    /// <summary>Onay butonu: parayı harcar ve objeyi yerleştirir.</summary>
    public bool ConfirmPlacement()
    {
        if (!IsPlacing) { Fail(null, PlacementFailReason.NoItem); return false; }

        BuildableItemSO item = _currentItem;
        if (_economy == null) { Fail(item, PlacementFailReason.NotReady); return false; }
        if (!IsValid) { Fail(item, PlacementFailReason.InvalidPosition); return false; }
        if (!_economy.TrySpendMoney(item.Price)) { Fail(item, PlacementFailReason.NotEnoughMoney); return false; }

        // Tek seferlik satın alma olayı: oyun döngüsünde değil
        GameObject placed = SpawnInternal(item, _position, CurrentRotation());

        Debug.Log($"[Placement] Yerleştirildi: {item.DisplayName} @ {_position}");
        ItemPlaced?.Invoke(item, placed);

        if (GameManager.Instance != null) GameManager.Instance.SaveNow();

        if (item.KeepPlacingAfterBuild && _economy.CanAfford(item.Price))
        {
            // Yeni obje bu noktayı kapattı: önizleme kırmızıya döner, oyuncu kaydırıp devam eder
            Physics.SyncTransforms();
            Revalidate(force: true);
        }
        else
        {
            EndPlacement();
        }
        return true;
    }

    /// <summary>
    /// Kayıttan geri yükleme ve başlangıç dükkanı için: para harcamaz, ItemPlaced tetiklemez,
    /// çakışma kontrolü yapmaz (konum zaten geçerli kaydedilmiştir).
    /// </summary>
    public GameObject SpawnWithoutCost(BuildableItemSO item, Vector3 position, int rotationSteps)
    {
        if (item == null || item.Prefab == null)
        {
            Debug.LogWarning("[Placement] SpawnWithoutCost: eşya veya prefab yok.", this);
            return null;
        }
        if (placedItemsRoot == null) placedItemsRoot = transform; // Awake'ten önce çağrılırsa

        position.y = groundHeight;
        return SpawnInternal(item, position, Quaternion.Euler(0f, 90f * (rotationSteps & 3), 0f));
    }

    private GameObject SpawnInternal(BuildableItemSO item, Vector3 position, Quaternion rotation)
    {
        GameObject placed = Instantiate(item.Prefab, position, rotation, placedItemsRoot);
        placed.name = item.DisplayName;

        // Table/Station bileşenleri OnEnable'da kendini kayıt defterine ekler (Table.All, StationRegistry)
        return placed;
    }

    /// <summary>İptal butonu.</summary>
    public void CancelPlacement()
    {
        if (!IsPlacing) return;
        Debug.Log($"[Placement] İptal: {_currentItem.DisplayName}");
        EndPlacement();
    }

    // ---------------- Girdi ----------------

    private void HandleInput()
    {
        int touchCount = Input.touchCount;
        if (touchCount > 0)
        {
            // İki parmak: kamera zoom/pan'e bırak
            if (touchCount > 1) { _activeFingerId = -1; return; }

            Touch touch = Input.GetTouch(0); // struct, allocation yok
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsPointerOverUI(touch.fingerId)) return;
                    _activeFingerId = touch.fingerId;
                    MoveToScreenPoint(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == _activeFingerId) MoveToScreenPoint(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == _activeFingerId) _activeFingerId = -1;
                    break;
            }
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // Editor'de fare ile test
        if (Input.GetMouseButtonDown(0)) _mouseBlockedByUI = IsPointerOverUI(-1);
        if (Input.GetMouseButton(0) && !_mouseBlockedByUI) MoveToScreenPoint(Input.mousePosition);
#endif
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        EventSystem es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject(pointerId);
    }

    private void MoveToScreenPoint(Vector2 screenPosition)
    {
        if (TryGetGroundPoint(screenPosition, out Vector3 point)) SetPosition(point, force: false);
    }

    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        if (_groundPlane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        point = default;
        return false;
    }

    // ---------------- Konum, ızgara, doğrulama ----------------

    private void SetPosition(Vector3 worldPoint, bool force)
    {
        Vector3 target = _currentItem.SnapToGrid ? SnapToGrid(worldPoint) : worldPoint;
        target.y = groundHeight;

        if (!force && (target - _position).sqrMagnitude < 0.000001f) return; // Değişiklik yok

        _position = target;
        _preview.Transform.SetPositionAndRotation(_position, CurrentRotation());
        Revalidate(force);
    }

    private Vector3 SnapToGrid(Vector3 p)
    {
        Vector2Int fp = RotatedFootprint();
        p.x = SnapAxis(p.x - gridOrigin.x, fp.x) + gridOrigin.x;
        p.z = SnapAxis(p.z - gridOrigin.z, fp.y) + gridOrigin.z;
        return p;
    }

    // Tek sayılı genişlik hücre ortasına, çift sayılı genişlik hücre çizgisine hizalanır
    private float SnapAxis(float value, int cells)
    {
        return (cells & 1) == 0
            ? Mathf.Round(value / cellSize) * cellSize
            : Mathf.Floor(value / cellSize) * cellSize + cellSize * 0.5f;
    }

    private Vector2Int RotatedFootprint()
    {
        Vector2Int fp = _currentItem.Footprint;
        return (_rotationSteps & 1) == 1 ? new Vector2Int(fp.y, fp.x) : fp;
    }

    private Quaternion CurrentRotation() => Quaternion.Euler(0f, 90f * _rotationSteps, 0f);

    private void Revalidate(bool force)
    {
        Vector2Int fp = RotatedFootprint();
        float halfX = fp.x * cellSize * 0.5f;
        float halfZ = fp.y * cellSize * 0.5f;

        bool inside =
            _position.x - halfX >= buildArea.min.x && _position.x + halfX <= buildArea.max.x &&
            _position.z - halfZ >= buildArea.min.z && _position.z + halfZ <= buildArea.max.z;

        bool valid = inside;
        if (inside)
        {
            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.01f, halfX - overlapPadding),
                overlapHeight * 0.5f,
                Mathf.Max(0.01f, halfZ - overlapPadding));
            // Kutu zeminin biraz üstünden başlar: zemin collider'ına değmesin
            Vector3 center = _position + new Vector3(0f, overlapHeight * 0.5f + 0.01f, 0f);

            int hits = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapBuffer, Quaternion.identity,
                blockingLayers, QueryTriggerInteraction.Ignore);
            valid = hits == 0;
        }

        if (!force && valid == IsValid) return;

        IsValid = valid;
        ApplyPreviewColor(valid ? validColor : invalidColor);
        ValidityChanged?.Invoke(valid);
    }

    private void ApplyPreviewColor(Color color)
    {
        Renderer[] renderers = _preview.Renderers;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorId, color);
            r.SetPropertyBlock(_propertyBlock); // Materyal kopyalanmaz, batching korunur
        }
    }

    // ---------------- Önizleme önbelleği ----------------

    private PreviewCache GetOrCreatePreview(BuildableItemSO item)
    {
        if (_previews.TryGetValue(item, out PreviewCache cached) && cached.Root != null) return cached;

        GameObject source = item.PreviewPrefab != null ? item.PreviewPrefab : item.Prefab;
        GameObject go = Instantiate(source, _previewParent);
        go.name = $"Preview_{item.DisplayName}";
        go.SetActive(false);

        // Önizleme oyun mantığı çalıştırmasın ve çakışma testine girmesin (tek seferlik)
        foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;

        var cache = new PreviewCache
        {
            Root = go,
            Transform = go.transform,
            Renderers = go.GetComponentsInChildren<Renderer>(true)
        };
        _previews[item] = cache;
        return cache;
    }

    private void EndPlacement()
    {
        if (_preview != null && _preview.Root != null) _preview.Root.SetActive(false);
        _currentItem = null;
        _preview = null;
        _activeFingerId = -1;
        IsValid = false;
        PlacementEnded?.Invoke();
    }

    private void Fail(BuildableItemSO item, PlacementFailReason reason)
    {
        Debug.LogWarning($"[Placement] Başarısız ({reason}): {(item != null ? item.DisplayName : "-")}");
        PlacementFailed?.Invoke(item, reason);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(buildArea.center.x, groundHeight, buildArea.center.z),
            new Vector3(buildArea.size.x, 0.01f, buildArea.size.z));
    }
#endif
}
