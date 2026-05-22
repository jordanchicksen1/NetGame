using UnityEngine;
using Unity.Netcode;
using System.Collections;

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

    // ================= DROPPED GEMS =================

    public void InitializeDrop(ulong ownerId)
    {
        ignorePlayerId = ownerId;
        ignoreTimer = 0.5f;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 force = new Vector2(Random.Range(-2f, 2f), 6f);
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    // ================= WORLD GEMS =================

    public void SetAsWorldGem()
    {
        isWorldGem = true;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Freeze briefly so spawn feels accurate
        rb.bodyType = RigidbodyType2D.Kinematic;

        StartCoroutine(EnablePhysicsAfterDelay());
    }

    IEnumerator EnablePhysicsAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    // ================= UPDATE =================

    void Update()
    {
        if (!IsServer) return;

        if (ignoreTimer > 0f)
        {
            ignoreTimer -= Time.deltaTime;
        }
    }

    // ================= COLLECTION =================

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (!collision.CompareTag("Player")) return;

        var netObj = collision.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // Prevent instant recollect by owner
        if (ignoreTimer > 0f &&
            netObj.OwnerClientId == ignorePlayerId)
            return;

        var player =
            collision.GetComponent<PlayerController2D>();

        if (player != null)
        {
            GemManager.Instance.OnGemCollected(
                player,
                isWorldGem
            );
        }

        GetComponent<NetworkObject>().Despawn();
    }
}