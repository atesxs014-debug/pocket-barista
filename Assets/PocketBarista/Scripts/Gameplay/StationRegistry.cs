using System.Collections.Generic;
using UnityEngine;

/// <summary>Sahnedeki aktif istasyonların listesi. Station kendini OnEnable/OnDisable'da kaydeder.</summary>
public static class StationRegistry
{
    private static readonly List<Station> Stations = new List<Station>(16);

    public static IReadOnlyList<Station> All => Stations;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Stations.Clear();

    public static void Register(Station s) { if (s != null && !Stations.Contains(s)) Stations.Add(s); }
    public static void Unregister(Station s) => Stations.Remove(s);

    public static bool HasStation(StationType type)
    {
        for (int i = 0; i < Stations.Count; i++)
            if (Stations[i] != null && Stations[i].Type == type) return true;
        return false;
    }

    /// <summary>Barista için: boşta, rezerve edilmemiş ve tipine uygun bekleyen siparişi olan istasyon.</summary>
    public static Station FindWorkable(OrderBoard board)
    {
        if (board == null) return null;
        for (int i = 0; i < Stations.Count; i++)
        {
            Station s = Stations[i];
            if (s != null && s.IsIdle && s.ReservedBy == null && board.HasPending(s.Type)) return s;
        }
        return null;
    }
}
