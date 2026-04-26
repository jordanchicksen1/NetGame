using UnityEngine;
using Unity.Netcode;

public class Gem : NetworkBehaviour
{
    Rigidbody2D rb;

    ulong ignorePlayerId;
    float ignoreTimer = 0.5f; // player can't re-collect for 0.5s

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void InitializeDrop(ulong ownerId)
    {
        ignorePlayerId = ownerId;

        // shoot upward + slight random sideways
        Vector2 force = new Vector2(Random.Range(-2f, 2f), 6f);
        rb.linearVelocity = force;
    }

    void Update()
    {
        if (!IsServer) return;

        if (ignoreTimer > 0f)
        {
            ignoreTimer -= Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (!collision.CompareTag("Player")) return;

        var netObj = collision.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // ignore original owner briefly
        if (ignoreTimer > 0f && netObj.OwnerClientId == ignorePlayerId)
            return;

        var player = collision.GetComponent<PlayerController2D>();

        if (player != null)
        {
            GemManager.Instance.OnGemCollected(player);
        }

        GetComponent<NetworkObject>().Despawn();
    }
}