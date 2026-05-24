using UnityEngine;
using Unity.Netcode;

public class SpikeHazard : NetworkBehaviour
{
    [Header("Knockback Direction")]
    [SerializeField] Vector2 knockbackDirection = Vector2.up;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (!collision.CompareTag("Player"))
            return;

        PlayerController2D player =
            collision.GetComponent<PlayerController2D>();

        if (player != null)
        {
            player.TakeSpikeHit(knockbackDirection);
        }
    }
}