using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 5f;
    [SerializeField] int maxBounces = 3;

    Rigidbody2D rb;
    int bounceCount;
    float direction;
    GameObject owner;

    public void Initialize(float dir, GameObject ownerObject)
    {
        direction = dir;
        owner = ownerObject;

        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore owner
        if (collision.gameObject == owner) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            bounceCount++;

            if (bounceCount >= maxBounces)
            {
                Destroy(gameObject);
            }
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController2D player = collision.gameObject.GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.LoseSpell();
            }

            Destroy(gameObject);
        }
    }
}