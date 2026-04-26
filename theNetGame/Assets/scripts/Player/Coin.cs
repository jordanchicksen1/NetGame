using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{
    bool collected = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collected) return;
        if (!collision.CompareTag("Player")) return;

        var netObj = collision.GetComponent<NetworkObject>();
        if (netObj == null) return;

        collected = true;

        ulong playerId = netObj.OwnerClientId;

        CollectCoin(playerId);
    }

    void CollectCoin(ulong playerId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
        var player = playerObj.GetComponent<PlayerController2D>();

        if (player != null)
        {
            player.AddCoin();
        }

        // hide coin instead of despawning
        gameObject.SetActive(false);
    }

    // called when gem resets the map
    public void ResetCoin()
    {
        if (!IsServer) return;

        collected = false;
        gameObject.SetActive(true);
    }
}