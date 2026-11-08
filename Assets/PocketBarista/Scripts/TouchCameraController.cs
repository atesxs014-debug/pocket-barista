using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Dikey ekran için dokunmatik kamera: tek parmakla kaydırma (ataletli), iki parmakla yakınlaştırma + kaydırma.
/// Yerleştirme modunda tek parmak kamerayı değil eşyayı hareket ettirir; iki parmak her zaman kameraya aittir.
/// Kamera açısı sahnede ayarlanır (ör. X=50°, Y=45°); bu script sadece odak noktasını ve zoom'u yönetir.
/// Ortografik kamerada zoom = orthographicSize, perspektifte zoom = odak noktasına uzaklık.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class TouchCameraController : MonoBehaviour
{
    [SerializeField, Tooltip("Yerleştirme modunda tek parmak pan'i kapatmak için")]
    private BuildingPlacementManager placementManager;

    [SerializeField] private float groundHeight = 0f;

    [Header("Sınırlar")]
    [SerializeField, Tooltip("Odak noktasının gidebileceği alan (XZ). Dükkan büyüyünce SetBounds ile genişletilir.")]
    private Bounds panBounds = new Bounds(Vector3.zero, new Vector3(16f, 0f, 16f));

    [Header("Zoom")]
    [SerializeField] private float minZoom = 4f;
    [SerializeField] private float maxZoom = 14f;
    [SerializeField, Tooltip("Editor: fare tekerleği hızı")] private float mouseScrollSpeed = 2f;

    [Header("His")]
    [SerializeField, Tooltip("Bu kadar piksel kaymadan pan başlamaz (masaya dokunma ile karışmasın)")]
    private float dragThresholdPixels = 12f;
    [SerializeField, Tooltip("Parmak bırakıldıktan sonra kaymanın sönme hızı")]
    private float inertiaDamping = 6f;

    public bool IsInteracting => _panning || _pinching;

    private Camera _camera;
    private Transform _transform;
    private Plane _groundPlane;

    private Vector3 _focus;
    private float _zoom;
    private float _orthoDistance;
    private Vector3 _velocity;

    private bool _panning;
    private int _panFingerId = -1;
    private Vector2 _panStartScreen;
    private Vector2 _prevScreen;

    private bool _pinching;
    private float _lastPinchDistance;
    private Vector2 _prevPinchMid;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _transform = transform;
        _groundPlane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));

        Ray center = new Ray(_transform.position, _transform.forward);
        if (_groundPlane.Raycast(center, out float distance))
        {
            _focus = center.GetPoint(distance);
        }
        else
        {
            Debug.LogError("[Camera] Kamera zemine bakmıyor; X açısını aşağı doğru ayarlayın. Odak (0,0,0) kabul edildi.", this);
            _focus = new Vector3(0f, groundHeight, 0f);
            distance = Vector3.Distance(_transform.position, _focus);
        }

        _orthoDistance = Mathf.Max(1f, distance);
        _zoom = _camera.orthographic ? _camera.orthographicSize : distance;

        if (minZoom > maxZoom) { float t = minZoom; minZoom = maxZoom; maxZoom = t; }
        Apply();
    }

    private void OnValidate()
    {
        if (minZoom < 0.5f) minZoom = 0.5f;
        if (maxZoom < minZoom) maxZoom = minZoom;
        if (dragThresholdPixels < 0f) dragThresholdPixels = 0f;
        if (inertiaDamping < 0f) inertiaDamping = 0f;
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        bool placing = placementManager != null && placementManager.IsPlacing;
        int touchCount = Input.touchCount;

        if (touchCount >= 2)
        {
            ResetPan();
            HandlePinch(Input.GetTouch(0), Input.GetTouch(1));
        }
        else
        {
            _pinching = false;
            if (touchCount == 1)
            {
                if (placing) ResetPan();
                else HandlePan(Input.GetTouch(0), dt);
            }
            else
            {
                ResetPan();
#if UNITY_EDITOR || UNITY_STANDALONE
                HandleMouse(placing, dt);
#endif
            }
        }

        // Atalet: parmak kalkınca hafifçe kaymaya devam eder
        if (!IsInteracting && _velocity.sqrMagnitude > 0.0001f)
        {
            _focus += _velocity * dt;
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, Mathf.Clamp01(inertiaDamping * dt));
        }

        Apply();
    }

    // ---------------- Public API ----------------

    /// <summary>Dükkan genişletildiğinde çağrılır.</summary>
    public void SetBounds(Bounds bounds) => panBounds = bounds;

    /// <summary>Kamerayı bir noktaya odaklar (ör. yeni yerleştirilen masa).</summary>
    public void FocusOn(Vector3 worldPoint)
    {
        _focus = worldPoint;
        _velocity = Vector3.zero;
        Apply();
    }

    // ---------------- Girdi ----------------

    private void HandlePan(Touch touch, float dt)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (IsPointerOverUI(touch.fingerId)) { _panFingerId = -1; return; }
                _panFingerId = touch.fingerId;
                _panStartScreen = touch.position;
                _prevScreen = touch.position;
                _panning = false;
                _velocity = Vector3.zero;
                break;

            case TouchPhase.Moved:
                if (touch.fingerId != _panFingerId) return;
                if (!_panning)
                {
                    if ((touch.position - _panStartScreen).sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
                    _panning = true;
                    _prevScreen = touch.position;
                }
                PanBetween(_prevScreen, touch.position, dt);
                _prevScreen = touch.position;
                break;

            case TouchPhase.Stationary:
                if (_panning) _velocity = Vector3.zero;
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (touch.fingerId == _panFingerId) { _panFingerId = -1; _panning = false; }
                break;
        }
    }

    private void HandlePinch(Touch a, Touch b)
    {
        float distance = (a.position - b.position).magnitude;
        Vector2 mid = (a.position + b.position) * 0.5f;

        if (!_pinching || a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
        {
            _pinching = true;
            _lastPinchDistance = distance;
            _prevPinchMid = mid;
            _velocity = Vector3.zero;
            return;
        }

        if (distance > 1f && _lastPinchDistance > 1f)
        {
            _zoom *= _lastPinchDistance / distance; // Parmaklar açılınca yakınlaşır
        }

        PanBetween(_prevPinchMid, mid, 0f); // İki parmakla sürükleme de kaydırır, atalet yok
        _lastPinchDistance = distance;
        _prevPinchMid = mid;
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void HandleMouse(bool placing, float dt)
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) _zoom -= scroll * mouseScrollSpeed;

        // Sağ tık her zaman, sol tık sadece yerleştirme modu dışında kaydırır
        bool down = Input.GetMouseButtonDown(1) || (!placing && Input.GetMouseButtonDown(0));
        bool held = Input.GetMouseButton(1) || (!placing && Input.GetMouseButton(0));
        Vector2 mouse = Input.mousePosition;

        if (down)
        {
            if (IsPointerOverUI(-1)) { _panFingerId = -1; return; }
            _panFingerId = 0;
            _panStartScreen = mouse;
            _prevScreen = mouse;
            _velocity = Vector3.zero;
        }
        else if (held && _panFingerId == 0)
        {
            if (!_panning && (mouse - _panStartScreen).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
            {
                _panning = true;
                _prevScreen = mouse;
            }
            if (_panning)
            {
                PanBetween(_prevScreen, mouse, dt);
                _prevScreen = mouse;
            }
        }
        else if (!held)
        {
            _panFingerId = -1;
            _panning = false;
        }
    }
#endif

    private void ResetPan()
    {
        _panFingerId = -1;
        _panning = false;
    }

    // Parmağın altındaki zemin noktası parmakla birlikte hareket eder
    private void PanBetween(Vector2 fromScreen, Vector2 toScreen, float dt)
    {
        if (!TryGetGroundPoint(fromScreen, out Vector3 from) || !TryGetGroundPoint(toScreen, out Vector3 to)) return;

        Vector3 delta = from - to;
        delta.y = 0f;
        _focus += delta;
        if (dt > 0f) _velocity = delta / dt;
    }

    private bool TryGetGroundPoint(Vector2 screen, out Vector3 point)
    {
        Ray ray = _camera.ScreenPointToRay(screen);
        if (_groundPlane.Raycast(ray, out float d))
        {
            point = ray.GetPoint(d);
            return true;
        }
        point = default;
        return false;
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        EventSystem es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject(pointerId);
    }

    private void Apply()
    {
        _zoom = Mathf.Clamp(_zoom, minZoom, maxZoom);

        _focus.x = Mathf.Clamp(_focus.x, panBounds.min.x, panBounds.max.x);
        _focus.z = Mathf.Clamp(_focus.z, panBounds.min.z, panBounds.max.z);
        _focus.y = groundHeight;

        if (_camera.orthographic)
        {
            _camera.orthographicSize = _zoom;
            _transform.position = _focus - _transform.forward * _orthoDistance;
        }
        else
        {
            _transform.position = _focus - _transform.forward * _zoom;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireCube(new Vector3(panBounds.center.x, groundHeight, panBounds.center.z),
            new Vector3(panBounds.size.x, 0.01f, panBounds.size.z));
    }
#endif
}
