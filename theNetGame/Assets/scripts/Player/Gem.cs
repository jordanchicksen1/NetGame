using UnityEngine;
using Unity.Netcode;

public class Gem : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            var player = collision.GetComponent<PlayerController2D>();

            if (player != null)
            {
                GemManager.Instance.OnGemCollected(player);
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}