using System;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 10f;
    [SerializeField] float gravity = 25f;
    [SerializeField] float bounceForce = 12f;

    [Header("Bounce")]
    [SerializeField] int maxBounces = 3;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float forwardCheckDistance = 0.1f;

    float direction;
    float verticalVelocity;

    int bounceCount;
    ulong ownerClientId;

    [SerializeField] StatusEffectType effectType;

    public void Initialize(float dir, ulong ownerId)
    {
        direction = dir;
        ownerClientId = ownerId;
        verticalVelocity = bounceForce;
    }

    void Update()
    {
        if (!IsServer) return;
        verticalVelocity -= gravity * Time.deltaTime;

        Vector2 movement = new Vector2(
            direction * speed,
            verticalVelocity
        );

        Vector2 currentPosition = transform.position;
        Vector2 nextPosition = currentPosition + movement * Time.deltaTime;

        // --- DOWNWARD CHECK (GROUND) ---
        RaycastHit2D groundHit = Physics2D.Raycast(
            currentPosition,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (verticalVelocity < 0 && groundHit.collider != null)
        {
            float groundY = groundHit.point.y;

            if (nextPosition.y <= groundY)
            {
                Bounce();
                nextPosition.y = groundY + 0.05f;
            }
        }

        // --- FORWARD CHECK (WALLS) ---
        Vector2 forwardDir = direction > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D wallHit = Physics2D.Raycast(
            currentPosition,
            forwardDir,
            forwardCheckDistance,
            groundLayer
        );

        if (wallHit.collider != null)
        {
            // Hit wall → destroy projectile
            if (IsServer)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(nextPosition, 0.5f);

        foreach (var hit in hits)
        {
            Debug.Log($"Projectile found: {hit.name}");

            HidingSpot hidingSpot =
                hit.GetComponent<HidingSpot>();

            if (hidingSpot != null)
            {
                Debug.Log("PROJECTILE HIT HIDING SPOT");

                hidingSpot.BreakSpot();

                GetComponent<NetworkObject>().Despawn();

                return;
            }
        }

        transform.position = nextPosition;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        GameObject hitObject = collision.gameObject;

        var netObj = hitObject.GetComponent<NetworkObject>();

        if (netObj != null && netObj.OwnerClientId == ownerClientId)
        {
            return;
        }

        HidingSpot hidingSpot = hitObject.GetComponent<HidingSpot>();

        if (hidingSpot != null)
        {
            hidingSpot.BreakSpot();

            GetComponent<NetworkObject>().Despawn();

            return;
        }

        // Player hit
        if (hitObject.CompareTag("Player"))
        {
            PlayerController2D player = hitObject.GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.LoseSpell();
                player.ApplyEffect(effectType);
                player.DropGem();
            }

            if (IsServer)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            return;
        }

        // Ground hit → bounce
        if (hitObject.CompareTag("Ground"))
        {
            Bounce();
        }
    }

    void Bounce()
    {
        bounceCount++;

        if (bounceCount >= maxBounces)
        {
            if (IsServer)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            return;
        }

        // Reset vertical velocity for consistent arc
        verticalVelocity = bounceForce;
    }
}