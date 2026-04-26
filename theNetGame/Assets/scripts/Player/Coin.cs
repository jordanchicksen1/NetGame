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

        // 👇 tell ALL clients to hide coin
        HideCoinClientRpc();
    }

    [ClientRpc]
    void HideCoinClientRpc()
    {
        gameObject.SetActive(false);
    }

    // called when gem resets the map
    public void ResetCoin()
    {
        if (!IsServer) return;

        collected = false;

        // tell ALL clients to show coin again
        ShowCoinClientRpc();
    }

    [ClientRpc]
    void ShowCoinClientRpc()
    {
        gameObject.SetActive(true);
    }
}