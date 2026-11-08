using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tek müşterilik masa. Müşteri ayrılınca masaya para yığını bırakır; yığın toplanmadan masa yeni müşteri almaz.
/// Dokunma: para yığını varsa toplar, yoksa bu masanın hazır siparişini servis eder (oyuncu garson gibi).
/// Kendini statik listeye kaydeder; spawner ve kasiyer buradan bulur.
/// </summary>
[DisallowMultipleComponent]
public sealed class Table : MonoBehaviour, IPointerClickHandler
{
    private static readonly List<Table> ActiveTables = new List<Table>(32);
    public static IReadOnlyList<Table> All => ActiveTables;

    [SerializeField, Tooltip("Müşterinin oturduğu nokta")] private Transform seatPoint;
    [SerializeField, Tooltip("Servis edilen ürünün konduğu nokta")] private Transform itemAnchor;
    [SerializeField, Tooltip("Para yığınının çıktığı nokta")] private Transform pileAnchor;

    public event Action<Table> Freed;

    public CustomerAI Occupant { get; private set; }
    public MoneyPile Pile { get; private set; }
    public bool IsFree => Occupant == null && Pile == null && isActiveAndEnabled;
    public Vector3 SeatPosition => seatPoint != null ? seatPoint.position : transform.position;
    public Quaternion SeatRotation => seatPoint != null ? seatPoint.rotation : transform.rotation;
    public Transform ItemAnchor => itemAnchor != null ? itemAnchor : transform;
    public Vector3 PilePosition => pileAnchor != null ? pileAnchor.position : transform.position + Vector3.up * 0.76f;
    public Vector3 ServicePosition => transform.position + (SeatPosition - transform.position).normalized * -0.7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveTables.Clear();

    private void OnEnable()
    {
        if (!ActiveTables.Contains(this)) ActiveTables.Add(this);
    }

    private void OnDisable()
    {
        ActiveTables.Remove(this);
        if (Occupant != null) Occupant.ForceLeave();
        if (Pile != null && gameObject.scene.isLoaded) Pile.Collect(); // Masa kaldırılırsa para kaybolmasın (sahne kapanırken değil)
    }

    public bool TryReserve(CustomerAI customer)
    {
        if (customer == null || !IsFree) return false;
        Occupant = customer;
        return true;
    }

    public void Release(CustomerAI customer)
    {
        if (Occupant != customer) return;
        Occupant = null;
        if (IsFree) Freed?.Invoke(this);
    }

    public void AttachPile(MoneyPile pile) => Pile = pile;

    public void DetachPile(MoneyPile pile)
    {
        if (Pile != pile) return;
        Pile = null;
        if (IsFree) Freed?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Pile != null && Pile.ReservedBy == null)
        {
            Pile.Collect();
            return;
        }

        OrderBoard board = OrderBoard.Instance;
        if (board == null) return;
        Order ready = board.FindReadyForTable(this);
        if (ready != null && board.Deliver(ready)) AudioManager.Play(SfxType.Serve);
    }
}
