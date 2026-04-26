using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{
    bool collected = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected) return; // prevents double triggers

        if (!collision.CompareTag("Player")) return;

        var netObj = collision.GetComponent<NetworkObject>();
        if (netObj == null) return;

        collected = true; // lock immediately

        CollectCoinRpc(netObj.OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CollectCoinRpc(ulong playerId)
    {
        if (!IsServer) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
        var player = playerObj.GetComponent<PlayerController2D>();

        if (player != null)
        {
            player.AddCoin();
        }

        GetComponent<NetworkObject>().Despawn();
    }
}