using Unity.Netcode;
using UnityEngine;

public class HidingSpot : NetworkBehaviour
{
    public PlayerController2D Occupant { get; private set; }

    public bool IsOccupied => Occupant != null;

    public bool TryHide(PlayerController2D player)
    {
        Debug.Log($"TryHide | IsServer={IsServer}");

        if (IsOccupied)
            return false;

        Occupant = player;
        return true;
    }

    public void ExitHide()
    {
        Occupant = null;
    }



    public void BreakSpot()
    {
        Debug.Log($"BreakSpot called. Occupant is null? {Occupant == null}");

        if (!IsServer)
            return;

        if (Occupant == null)
            return;

        Occupant.ForceExitHide();

        GetComponent<NetworkObject>().Despawn();
    }

    public void SetOccupant(PlayerController2D player)
    {
        Occupant = player;
    }
}