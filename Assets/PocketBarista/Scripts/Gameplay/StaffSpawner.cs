using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// İşe alınmış personeli sahnede oluşturur: açılışta kayıttakiler, oyun sırasında yeni işe alınan (tek seferlik olay).
/// Her rolün bekleme noktası ayrı (barista tezgah arkası, garson tezgah önü, kasiyer kasa yanı).
/// </summary>
[DisallowMultipleComponent]
public sealed class StaffSpawner : MonoBehaviour
{
    [SerializeField] private Transform baristaHome;
    [SerializeField] private Transform waiterHome;
    [SerializeField] private Transform cashierHome;
    [SerializeField, Tooltip("Aynı roldeki personel arası yan kayma")] private float spacing = 0.6f;

    private readonly List<StaffAI> _spawned = new List<StaffAI>(8);
    private GameServices _services;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { Debug.LogError("[StaffSpawner] Services yok.", this); enabled = false; return; }

        IReadOnlyList<StaffSO> hired = _services.Staff.Hired;
        for (int i = 0; i < hired.Count; i++) Spawn(hired[i]);
        _services.Staff.StaffHired += Spawn;
    }

    private void OnDestroy()
    {
        if (_services != null) _services.Staff.StaffHired -= Spawn;
    }

    private void Spawn(StaffSO data)
    {
        if (data == null || data.Prefab == null) { Debug.LogWarning("[StaffSpawner] Prefab'sız personel atlandı.", this); return; }

        int sameRole = 0;
        for (int i = 0; i < _spawned.Count; i++) if (_spawned[i] != null && _spawned[i].Role == data.Role) sameRole++;

        Transform home = data.Role == StaffRole.Barista ? baristaHome : data.Role == StaffRole.Waiter ? waiterHome : cashierHome;
        Vector3 basePos = home != null ? home.position : transform.position;
        Vector3 offset = (home != null ? home.right : Vector3.right) * spacing * sameRole;

        StaffAI staff = Instantiate(data.Prefab, basePos + offset, home != null ? home.rotation : Quaternion.identity, transform);
        staff.name = data.Id;
        staff.Init(data, _services, basePos + offset);
        _spawned.Add(staff);
        Debug.Log($"[StaffSpawner] {data.Id} sahneye geldi.", this);
    }
}
