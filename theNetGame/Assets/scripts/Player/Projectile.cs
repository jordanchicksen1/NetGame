using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 5f;
    [SerializeField] int maxBounces = 3;

    Rigidbody2D rb;
    int bounceCount;
    float direction;

    public void Initialize(float dir)
    {
        direction = dir;

        rb = GetComponent<Rigidbody2D>();

        // Initial forward velocity
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Bounce off ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            bounceCount++;

            if (bounceCount >= maxBounces)
            {
                Destroy(gameObject);
            }
        }

        // Hit player
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