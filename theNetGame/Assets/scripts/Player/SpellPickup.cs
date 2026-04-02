using UnityEngine;

public class SpellPickup : MonoBehaviour
{
    [SerializeField] SpellType spellType;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController2D player = collision.GetComponent<PlayerController2D>();

            if (player != null)
            {
                player.SetSpell(spellType);
            }

            Destroy(gameObject);
        }
    }
}