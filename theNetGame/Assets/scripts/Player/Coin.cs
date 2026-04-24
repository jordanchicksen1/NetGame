using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            PlayerController2D player = collision.GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.AddCoin();
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}