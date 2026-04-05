using UnityEngine;

public class Projectile : MonoBehaviour
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
    GameObject owner;

    [SerializeField] StatusEffectType effectType;

    public void Initialize(float dir, GameObject ownerObject)
    {
        direction = dir;
        owner = ownerObject;

        // Initial upward arc
        verticalVelocity = bounceForce;
    }

    void Update()
    {
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
            Destroy(gameObject);
            return;
        }

        transform.position = nextPosition;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject hitObject = collision.gameObject;

        // Ignore owner if exists
        if (owner != null && hitObject == owner) return;

        // Player hit
        if (hitObject.CompareTag("Player"))
        {
            PlayerController2D player = hitObject.GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.LoseSpell();
                player.ApplyEffect(effectType);
            }

            Destroy(gameObject);
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
            Destroy(gameObject);
            return;
        }

        // Reset vertical velocity for consistent arc
        verticalVelocity = bounceForce;
    }
}