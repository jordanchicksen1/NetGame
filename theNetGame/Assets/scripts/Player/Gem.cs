using UnityEngine;
using Unity.Netcode;

public class Gem : NetworkBehaviour
{
    Rigidbody2D rb;

    ulong ignorePlayerId;
    float ignoreTimer = 0.5f;

    bool isWorldGem = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // called when gem is dropped from player
    public void InitializeDrop(ulong ownerId)
    {
        ignorePlayerId = ownerId;
        ignoreTimer = 0.5f;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = Vector2.zero;

        Vector2 force = new Vector2(Random.Range(-2f, 2f), 6f);
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    // called when gem is spawned in the world
    public void SetAsWorldGem()
    {
        isWorldGem = true;
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

        // prevent instant re-collection by same player
        if (ignoreTimer > 0f && netObj.OwnerClientId == ignorePlayerId)
            return;

        var player = collision.GetComponent<PlayerController2D>();

        if (player != null)
        {
            GemManager.Instance.OnGemCollected(player, isWorldGem);
        }

        GetComponent<NetworkObject>().Despawn();
    }
}