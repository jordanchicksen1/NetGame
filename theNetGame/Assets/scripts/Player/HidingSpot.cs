using Unity.Netcode;
using UnityEngine;

public class HidingSpot : NetworkBehaviour
{
    public PlayerController2D Occupant { get; private set; }

    public bool IsOccupied => Occupant != null;

    public bool TryHide(PlayerController2D player)
    {
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
        if (!IsServer)
            return;

        if (Occupant != null)
        {
            Occupant.ForceExitHide();
        }

        GetComponent<NetworkObject>().Despawn();
    }
}